using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ghuri.Infrastructure.Persistence.Seed;

/// <summary>
/// DEVELOPMENT ONLY: fills the catalogue with realistic sample categories and
/// destinations - national (Bangladesh) and international - plus four
/// published, bookable packages (DemoPackageSeeder), so the admin tables,
/// public pages and checkout have something to work with.
/// </summary>
/// <remarks>
/// <para>
/// Run with "dotnet run --project src/Ghuri.Api -- seed-demo"; Program.cs
/// refuses it outside Development, so sample data can never reach a real
/// server. Safe to run again: a row whose slug already exists is skipped,
/// so it never duplicates and never overwrites an admin's edits.
/// </para>
/// <para>
/// Every destination gets a gallery of generated postcards (DemoPostcard):
/// a cover plus one photo per highlight. A demo destination that is already
/// there but has NO photos (seeded before galleries existed) gets them on
/// the next run; one with photos - e.g. real ones uploaded in the admin -
/// is left alone.
/// </para>
/// </remarks>
internal sealed class DemoDataSeeder(
    AppDbContext db,
    DemoPackageSeeder packageSeeder,
    DemoPhotoWriter photos,
    TimeProvider clock,
    ILogger<DemoDataSeeder> logger)
{
    private sealed record DemoCategory(string Name, string Icon);

    /// <summary>The postcard's colours: sky at the top, then the horizon (the hills are a darker shade of it).</summary>
    private sealed record Look(string SkyTop, string SkyBottom);

    /// <summary>Highlights = three famous sights - one photo each, after the cover.</summary>
    private sealed record DemoDestination(
        string CountryIso, string Name, Look Look, string[] Highlights, string Summary, bool IsFeatured = false);

    // Declared before Destinations: static fields are set in the order they are written.
    private static readonly Look Sea = new("#0EA5E9", "#FDE68A");
    private static readonly Look Coral = new("#06B6D4", "#FBCFE8");
    private static readonly Look Hills = new("#6366F1", "#A7F3D0");
    private static readonly Look Tea = new("#16A34A", "#FEF3C7");
    private static readonly Look Forest = new("#0F766E", "#D9F99D");
    private static readonly Look Lake = new("#2563EB", "#BAE6FD");
    private static readonly Look Snow = new("#475569", "#F1F5F9");
    private static readonly Look Heritage = new("#B45309", "#FDE68A");
    private static readonly Look Desert = new("#F97316", "#FEF3C7");
    private static readonly Look Holy = new("#1E293B", "#FCD34D");
    private static readonly Look City = new("#1E3A8A", "#F9A8D4");

    // Icon = a lucide-react icon name (the frontend's icon set).
    // Names use "and", never "&".
    private static readonly DemoCategory[] Categories =
    [
        new("Beach", "umbrella"),
        new("Hill and Mountain", "mountain"),
        new("Honeymoon", "heart"),
        new("Umrah and Hajj", "moon-star"),
        new("Family", "users"),
        new("Adventure", "compass"),
        new("Nature and Wildlife", "trees"),
        new("Heritage and Culture", "landmark"),
        new("Cruise and Houseboat", "ship"),
        new("City Break", "building-2"),
        new("Camping and Trekking", "tent"),
        new("Luxury", "sparkles"),
        new("Weekend Getaway", "sun"),
        new("Corporate and Group", "briefcase"),
        new("Student Tour", "graduation-cap"),
    ];

    private static readonly DemoDestination[] Destinations =
    [
        // ---- National (Bangladesh) ----
        new("BD", "Cox's Bazar", Sea, ["Himchari", "Inani Beach", "Marine Drive"],
            "The world's longest natural sea beach - 120 km of golden sand along the Bay of Bengal, with Himchari, Inani and Marine Drive.", true),
        new("BD", "Saint Martin's Island", Coral, ["Chhera Dwip", "Coral Beach", "West Beach Sunset"],
            "Bangladesh's only coral island: turquoise water, coconut palms, Chhera Dwip and fresh seafood on the beach.", true),
        new("BD", "Sylhet", Tea, ["Ratargul Swamp Forest", "Jaflong", "Bholaganj Sada Pathor"],
            "Tea gardens, the Ratargul swamp forest, Jaflong's stone-filled river and the clear water of Bholaganj Sada Pathor.", true),
        new("BD", "Sajek Valley", Hills, ["Konglak Para", "Sea of Clouds", "Ruilui Para"],
            "The 'queen of hills' in Rangamati - wake up above a sea of clouds, with Konglak Para at sunset.", true),
        new("BD", "Sundarbans", Forest, ["Kotka Beach", "Karamjal", "Hiron Point"],
            "The largest mangrove forest on Earth and home of the Royal Bengal tiger, explored on a 3-day launch trip.", true),
        new("BD", "Bandarban", Hills, ["Nilgiri", "Nafakhum Waterfall", "Boga Lake"],
            "Nilgiri, Nafakhum waterfall, Boga Lake and Keokradong - the highest hills and wildest trails in Bangladesh."),
        new("BD", "Srimangal", Tea, ["Tea Estates", "Lawachara Rainforest", "Seven-Layer Tea"],
            "The tea capital of Bangladesh: endless tea estates, Lawachara rainforest and the famous seven-layer tea."),
        new("BD", "Rangamati", Lake, ["Kaptai Lake", "Hanging Bridge", "Shuvolong Waterfall"],
            "Kaptai Lake by boat, the hanging bridge, Shuvolong waterfall and the culture of the Chittagong Hill Tracts."),
        new("BD", "Khagrachari", Hills, ["Alutila Cave", "Risang Waterfall", "Hill Villages"],
            "Alutila cave, Risang waterfall and quiet hill villages - an easy add-on to Sajek."),
        new("BD", "Kuakata", Sea, ["Sunrise Beach", "Sunset Point", "Gangamati Forest"],
            "'Daughter of the sea' - one of the few beaches where you watch both the sunrise and the sunset."),
        new("BD", "Tanguar Haor", Lake, ["Houseboat Stay", "Watchtower", "Niladri Lake"],
            "A vast wetland in Sunamganj, best explored on a houseboat beneath the hills of Meghalaya."),
        new("BD", "Nijhum Dwip", Forest, ["Spotted Deer", "Mangrove Forest", "Empty Beaches"],
            "A remote island of spotted deer, mangroves and empty beaches at the mouth of the Meghna."),
        new("BD", "Chattogram", Sea, ["Patenga Beach", "Foy's Lake", "Bayazid Bostami Shrine"],
            "Patenga beach, Foy's Lake, Bayazid Bostami shrine and the gateway to the hill tracts."),
        new("BD", "Dhaka", City, ["Lalbagh Fort", "Ahsan Manzil", "Buriganga River"],
            "Old Dhaka's Lalbagh Fort, Ahsan Manzil, rickshaw rides on the Buriganga and legendary street food."),
        new("BD", "Sonargaon", Heritage, ["Panam City", "Folk Art Museum", "Goaldi Mosque"],
            "The ancient capital of Bengal - Panam City's abandoned mansions and the Folk Art Museum."),
        new("BD", "Bagerhat", Heritage, ["Sixty Dome Mosque", "Khan Jahan Ali's Tomb", "Mosque City"],
            "The Sixty Dome Mosque and Khan Jahan Ali's mosque city, a UNESCO World Heritage Site."),
        new("BD", "Paharpur", Heritage, ["Somapura Mahavihara", "Terracotta Plaques", "Site Museum"],
            "Somapura Mahavihara, one of the largest Buddhist monasteries south of the Himalaya (UNESCO)."),

        // ---- International ----
        new("MV", "Maldives", Coral, ["Overwater Villas", "House-Reef Snorkelling", "Sandbank Picnic"],
            "Overwater villas, house-reef snorkelling and powder-white beaches - the classic honeymoon.", true),
        new("TH", "Phuket", Sea, ["Phi Phi Islands", "James Bond Island", "Patong Beach"],
            "Andaman beaches, Phi Phi and James Bond islands by speedboat, and Patong's nightlife.", true),
        new("ID", "Bali", Forest, ["Ubud Rice Terraces", "Nusa Penida", "Tanah Lot"],
            "Temples, Ubud's rice terraces, Nusa Penida's cliffs and sunset beach clubs.", true),
        new("AE", "Dubai", Desert, ["Burj Khalifa", "Desert Safari", "Dhow Cruise"],
            "Burj Khalifa, desert safari with BBQ dinner, Dubai Mall and a dhow cruise on the Marina.", true),
        new("SA", "Makkah", Holy, ["Masjid al-Haram", "The Kaaba", "Jabal al-Nour"],
            "Masjid al-Haram and the Kaaba - the heart of every Umrah and Hajj journey.", true),
        new("SA", "Madinah", Holy, ["Masjid an-Nabawi", "Quba Mosque", "Mount Uhud"],
            "Masjid an-Nabawi, the Prophet's Mosque, with ziyarah to Quba and Mount Uhud."),
        new("NP", "Pokhara", Lake, ["Phewa Lake", "Sarangkot Paragliding", "Annapurna Sunrise"],
            "Phewa Lake, paragliding from Sarangkot and sunrise over the Annapurna range.", true),
        new("NP", "Kathmandu", Heritage, ["Durbar Square", "Swayambhunath", "Boudhanath Stupa"],
            "Durbar squares, Swayambhunath, Boudhanath stupa and the gateway to the Himalaya."),
        new("BT", "Thimphu and Paro", Hills, ["Tiger's Nest", "Punakha Dzong", "Dochula Pass"],
            "Tiger's Nest monastery, Punakha Dzong and Dochula Pass in the Land of the Thunder Dragon.", true),
        new("IN", "Kashmir", Snow, ["Dal Lake Shikara", "Gulmarg", "Pahalgam"],
            "Shikara rides on Dal Lake, Gulmarg's snow, Pahalgam's valleys and houseboat stays in Srinagar.", true),
        new("IN", "Darjeeling", Tea, ["Toy Train", "Tiger Hill", "Tea Gardens"],
            "The toy train, sunrise over Kanchenjunga from Tiger Hill and some of the world's finest tea."),
        new("IN", "Sikkim", Snow, ["Gangtok", "Tsomgo Lake", "Nathula Pass"],
            "Gangtok, Tsomgo Lake, Nathula Pass and the flower valleys of North Sikkim."),
        new("IN", "Goa", Sea, ["Baga Beach", "Old Goa Churches", "Spice Farms"],
            "Beaches, Portuguese churches, spice farms and famous beach shacks."),
        new("IN", "Rajasthan", Desert, ["Amber Fort", "Udaipur Lakes", "Jaisalmer Desert"],
            "The palaces and forts of Jaipur, Udaipur's lakes and camel rides in Jaisalmer's desert."),
        new("IN", "Delhi and Agra", Heritage, ["Taj Mahal", "Agra Fort", "Qutub Minar"],
            "The Taj Mahal at sunrise, Agra Fort, Qutub Minar and the lanes of Old Delhi."),
        new("TH", "Bangkok", City, ["Grand Palace", "Floating Market", "Rooftop Views"],
            "The Grand Palace, floating markets, rooftop views and endless street food."),
        new("TH", "Pattaya", Sea, ["Coral Island", "Nong Nooch Garden", "Walking Street"],
            "Coral Island, Nong Nooch garden and an easy beach escape from Bangkok."),
        new("MY", "Kuala Lumpur", City, ["Petronas Towers", "Batu Caves", "Genting Highlands"],
            "Petronas Towers, Batu Caves, Genting Highlands and great-value shopping."),
        new("MY", "Langkawi", Coral, ["SkyBridge", "Mangrove Tour", "Cenang Beach"],
            "Duty-free island of the SkyBridge, mangrove boat tours and quiet beaches."),
        new("SG", "Singapore", City, ["Marina Bay Sands", "Gardens by the Bay", "Sentosa"],
            "Marina Bay Sands, Gardens by the Bay, Sentosa and Universal Studios."),
        new("LK", "Kandy and Nuwara Eliya", Tea, ["Temple of the Tooth", "Hill-Country Train", "Tea Estates"],
            "The Temple of the Tooth, the scenic hill-country train and misty tea estates."),
        new("TR", "Istanbul", Heritage, ["Hagia Sophia", "Blue Mosque", "Bosphorus Cruise"],
            "Hagia Sophia, the Blue Mosque, the Grand Bazaar and a Bosphorus cruise between two continents."),
        new("TR", "Cappadocia", Desert, ["Hot-Air Balloons", "Fairy Chimneys", "Cave Hotels"],
            "Sunrise hot-air balloon rides over fairy chimneys and nights in a cave hotel."),
        new("VN", "Ha Long Bay", Coral, ["Limestone Islands", "Overnight Cruise", "Sung Sot Cave"],
            "Thousands of limestone islands in emerald water - best seen on an overnight cruise."),
        new("EG", "Cairo", Desert, ["Pyramids of Giza", "The Sphinx", "Nile Dinner Cruise"],
            "The Pyramids of Giza, the Sphinx, the Egyptian Museum and a dinner cruise on the Nile."),
        new("JP", "Tokyo and Kyoto", City, ["Shibuya Crossing", "Mount Fuji", "Fushimi Inari"],
            "Neon Shibuya, Mount Fuji day trips, Kyoto's temples and cherry blossom season."),
        new("KR", "Seoul", City, ["Gyeongbokgung Palace", "Nami Island", "Myeongdong"],
            "Gyeongbokgung Palace, Nami Island, K-culture streets and autumn colours."),
        new("CH", "Interlaken", Snow, ["Jungfraujoch", "Lake Brienz", "Swiss Mountain Train"],
            "Alpine lakes, Jungfraujoch - the 'Top of Europe' - and Swiss mountain trains."),
        new("FR", "Paris", City, ["Eiffel Tower", "The Louvre", "Montmartre"],
            "The Eiffel Tower, the Louvre, Montmartre and cafés along the Seine."),
        new("AU", "Sydney", Sea, ["Opera House", "Harbour Bridge", "Bondi Beach"],
            "The Opera House, Harbour Bridge climb, Bondi Beach and the Blue Mountains."),
        new("AZ", "Baku", City, ["Flame Towers", "Old City", "Gobustan"],
            "The Flame Towers, the old walled city and the mud volcanoes of Gobustan."),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            throw new InvalidOperationException(
                "The database has pending migrations. Run \".\\ef.cmd database update\" first, then seed.");

        var countries = await db.Countries.ToDictionaryAsync(c => c.IsoCode, cancellationToken);
        if (countries.Count == 0)
            throw new InvalidOperationException(
                "No countries found - run \"dotnet run --project src/Ghuri.Api -- seed\" first (it adds the countries).");

        var addedCategories = await AddCategoriesAsync(cancellationToken);
        var (addedDestinations, photographed) = await AddDestinationsAsync(countries, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // After the save above: packages need the destinations and categories to exist.
        var addedPackages = await packageSeeder.SeedAsync(cancellationToken);

        logger.LogInformation(
            "Demo data: added {Categories} categories, {Destinations} destinations, photo galleries for {Photographed} destinations "
            + "and {Packages} published packages (existing slugs were skipped).",
            addedCategories, addedDestinations, photographed, addedPackages);
    }

    private async Task<int> AddCategoriesAsync(CancellationToken cancellationToken)
    {
        var existing = (await db.Categories.Select(c => c.Slug).ToListAsync(cancellationToken)).ToHashSet();
        var added = 0;

        for (var i = 0; i < Categories.Length; i++)
        {
            var slug = Slug.Create(Categories[i].Name);
            if (existing.Contains(slug))
                continue;

            db.Categories.Add(Category.Create(Categories[i].Name, slug, Categories[i].Icon, sortOrder: (i + 1) * 10));
            added++;
        }

        return added;
    }

    /// <returns>How many destinations were added, and how many got a photo gallery (new ones + old ones that had none).</returns>
    private async Task<(int Added, int Photographed)> AddDestinationsAsync(
        Dictionary<string, Country> countries, CancellationToken cancellationToken)
    {
        // Images included: an existing destination with an empty gallery gets the demo photos.
        var existing = await db.Destinations.Include(d => d.Images).ToDictionaryAsync(d => d.Slug, cancellationToken);
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var added = 0;
        var photographed = 0;

        for (var i = 0; i < Destinations.Length; i++)
        {
            var demo = Destinations[i];
            if (!countries.TryGetValue(demo.CountryIso, out var country))
            {
                logger.LogWarning("Demo destination {Name} skipped: country {Iso} not found.", demo.Name, demo.CountryIso);
                continue;
            }

            var slug = Slug.Create(demo.Name);
            if (existing.TryGetValue(slug, out var destination))
            {
                if (destination.Images.Count > 0)
                    continue; // has photos already - never replace an admin's
            }
            else
            {
                destination = Destination.Create(
                    country.Id, demo.Name, slug, demo.Summary,
                    isFeatured: demo.IsFeatured, sortOrder: (i + 1) * 10,
                    seoTitle: $"{demo.Name} Tour Packages | Ghuri",
                    seoDescription: demo.Summary.Length <= 160 ? demo.Summary : demo.Summary[..157] + "...");
                db.Destinations.Add(destination);
                added++;
            }

            destination.SetImages(await DrawPhotosAsync(demo, country.Name, nowUtc, cancellationToken));
            photographed++;
        }

        return (added, photographed);
    }

    /// <summary>A cover (the destination's name) then one photo per highlight - four in all.</summary>
    private Task<List<Guid>> DrawPhotosAsync(DemoDestination demo, string countryName, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var place = $"{demo.Name}, {countryName}";
        List<(string Title, string Subtitle)> captions = [(demo.Name, countryName), .. demo.Highlights.Select(h => (h, place))];
        return photos.SaveAsync(demo.Name, captions, demo.Look.SkyTop, demo.Look.SkyBottom, nowUtc, cancellationToken);
    }
}
