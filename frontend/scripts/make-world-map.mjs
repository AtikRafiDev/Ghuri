// Builds src/shared/assets/world-map.svg - the faint world map behind the website's pages
// (shared/components/WorldMapBackdrop). Run it again only to change the map, e.g. other routes:
//
//   curl -L -o countries-110m.json https://cdn.jsdelivr.net/npm/world-atlas@2/countries-110m.json
//   node scripts/make-world-map.mjs countries-110m.json src/shared/assets/world-map.svg
//
// Map data: Natural Earth 1:110m (public domain), packed as TopoJSON by world-atlas (ISC).
// Nothing to install: plain Node.
import { readFileSync, writeFileSync } from 'node:fs'

const [input, output] = process.argv.slice(2)
const topo = JSON.parse(readFileSync(input, 'utf8'))
const { scale: [sx, sy], translate: [tx, ty] } = topo.transform

// 1. Arcs: delta-encoded, quantized -> [lon, lat].
const arcs = topo.arcs.map((arc) => {
  let x = 0
  let y = 0
  return arc.map(([dx, dy]) => {
    x += dx
    y += dy
    return [x * sx + tx, y * sy + ty]
  })
})

// 2. How many countries use each arc: 1 = coastline, 2 = a border between two countries. Antarctica is left out.
const uses = new Array(arcs.length).fill(0)
for (const g of topo.objects.countries.geometries) {
  if (g.properties?.name === 'Antarctica') continue
  const rings = g.type === 'Polygon' ? g.arcs : g.type === 'MultiPolygon' ? g.arcs.flat() : []
  for (const ring of rings) for (const i of ring) uses[i < 0 ? ~i : i]++
}

// 3. Natural Earth projection (the same formula as d3-geo's geoNaturalEarth1).
const rad = Math.PI / 180
function project([lon, lat]) {
  const l = lon * rad
  const p = lat * rad
  const p2 = p * p
  const p4 = p2 * p2
  return [
    l * (0.8707 - 0.131979 * p2 + p4 * (-0.013791 + p4 * (0.003971 * p2 - 0.001529 * p4))),
    -p * (1.007226 + p2 * (0.015085 + p4 * (-0.044475 + 0.028874 * p2 - 0.005916 * p4))), // SVG y grows downwards
  ]
}

const projected = arcs.map((arc) => arc.map(project))
let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity
projected.forEach((arc, i) => {
  if (!uses[i]) return
  for (const [x, y] of arc) {
    minX = Math.min(minX, x); maxX = Math.max(maxX, x)
    minY = Math.min(minY, y); maxY = Math.max(maxY, y)
  }
})

// The map is cropped on the left at the American west coast: west of it there is only the
// Pacific (and the tip of Alaska), which would leave the left side of the screen empty. So the
// land reaches every edge, and the backdrop can cover the whole screen (CSS: mask-size: cover).
const westEdge = -130
minX = project([westEdge, 0])[0]
const inView = (i) => projected[i].some(([x]) => x >= minX) // arcs entirely west of the crop are left out

const width = 1000
const pad = 0
const k = (width - 2 * pad) / (maxX - minX)
const height = Math.round((maxY - minY) * k + 2 * pad)
const toView = ([x, y]) => [(x - minX) * k + pad, (y - minY) * k + pad]
const fmt = (n) => (Math.round(n * 10) / 10).toString()

// Douglas-Peucker: drop points closer than `tolerance` (viewBox units) to the line through their neighbours.
function simplify(points, tolerance) {
  if (points.length < 3) return points
  const keep = new Array(points.length).fill(false)
  keep[0] = keep[points.length - 1] = true
  const stack = [[0, points.length - 1]]
  while (stack.length) {
    const [a, b] = stack.pop()
    const [ax, ay] = points[a]
    const [bx, by] = points[b]
    const len = Math.hypot(bx - ax, by - ay)
    let far = -1
    let farDist = tolerance
    for (let i = a + 1; i < b; i++) {
      const [px, py] = points[i]
      // A closed ring starts and ends on the same point: measure from that point instead of from a line.
      const dist = len > 1e-9 ? Math.abs((bx - ax) * (ay - py) - (ax - px) * (by - ay)) / len : Math.hypot(px - ax, py - ay)
      if (dist > farDist) {
        far = i
        farDist = dist
      }
    }
    if (far > 0) {
      keep[far] = true
      stack.push([a, far], [far, b])
    }
  }
  return points.filter((_, i) => keep[i])
}

// One arc -> SVG path data. The data is cut at the 180th meridian: never draw along that cut, or across the
// whole map where a line jumps from +180 to -180 - start a new piece there instead. Relative "l" commands
// between rounded points keep the file small without the rounding drifting.
function pathOf(arc) {
  const runs = [[]]
  arc.forEach((pt, i) => {
    const prev = arc[i - 1]
    const onCut = prev && Math.abs(prev[0]) > 179.9 && Math.abs(pt[0]) > 179.9
    const jumps = prev && Math.abs(pt[0] - prev[0]) > 180
    if (onCut || jumps) runs.push([])
    runs[runs.length - 1].push(toView(project(pt)))
  })
  let d = ''
  for (const run of runs) {
    const pts = simplify(run, 0.35).map(([x, y]) => [Math.round(x * 10), Math.round(y * 10)]) // tenths, as integers
    if (pts.length < 2) continue
    d += `M${pts[0][0] / 10} ${pts[0][1] / 10}`
    for (let i = 1; i < pts.length; i++) {
      const dx = (pts[i][0] - pts[i - 1][0]) / 10
      const dy = (pts[i][1] - pts[i - 1][1]) / 10
      if (dx || dy) d += `l${dx} ${dy}`.replace(/ -/g, '-')
    }
  }
  return d
}

const coast = arcs.filter((_, i) => uses[i] === 1 && inView(i)).map(pathOf).join('')
const borders = arcs.filter((_, i) => uses[i] >= 2 && inView(i)).map(pathOf).join('')

// 4. Routes from Dhaka to a few of the places Ghuri sells - gentle curves, dashed.
const dhaka = [90.41, 23.81]
const places = {
  Paris: [2.35, 48.86],
  Istanbul: [28.98, 41.01],
  Dubai: [55.27, 25.2],
  Maldives: [73.51, 4.17],
  Bali: [115.19, -8.41],
  Tokyo: [139.69, 35.69],
  Sydney: [151.21, -33.87],
}
const [hx, hy] = toView(project(dhaka))
const routes = Object.values(places).map((place) => {
  const [px, py] = toView(project(place))
  const dx = px - hx
  const dy = py - hy
  const len = Math.hypot(dx, dy)
  // Bend each curve to one side by a fifth of its length, always bowing upwards (north), like a flight path.
  let nx = -dy / len
  let ny = dx / len
  if (ny > 0) {
    nx = -nx
    ny = -ny
  }
  const cx = (hx + px) / 2 + nx * len * 0.2
  const cy = (hy + py) / 2 + ny * len * 0.2
  return { d: `M${fmt(hx)} ${fmt(hy)}Q${fmt(cx)} ${fmt(cy)} ${fmt(px)} ${fmt(py)}`, end: [px, py] }
})

const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height}" fill="none" stroke="#000" stroke-linecap="round" stroke-linejoin="round">
<!-- World map backdrop: coastlines, country borders and routes from Dhaka. Map data: Natural Earth (public domain), via world-atlas. Generated by scripts/make-world-map.mjs - do not edit by hand. -->
<path stroke-width="0.7" d="${coast}"/>
<path stroke-width="0.4" stroke-opacity="0.5" d="${borders}"/>
<g stroke-width="0.7" stroke-dasharray="2 3.5">${routes.map((r) => `<path d="${r.d}"/>`).join('')}</g>
<g fill="#000" stroke="none">${routes.map((r) => `<circle cx="${fmt(r.end[0])}" cy="${fmt(r.end[1])}" r="2"/>`).join('')}<circle cx="${fmt(hx)}" cy="${fmt(hy)}" r="3"/></g>
<circle cx="${fmt(hx)}" cy="${fmt(hy)}" r="7" stroke-width="0.8"/>
</svg>
`
writeFileSync(output, svg)
console.log(`viewBox 0 0 ${width} ${height}, ${(svg.length / 1024).toFixed(1)} KB, coast arcs ${uses.filter((u) => u === 1).length}, border arcs ${uses.filter((u) => u >= 2).length}`)
