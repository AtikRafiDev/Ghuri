using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ghuri.Infrastructure.Persistence.Seed;

/// <summary>
/// DEVELOPMENT ONLY: fills the catalogue with realistic sample categories and
/// destinations - national (Bangladesh) and international - so the admin
/// tables and public pages have something to show while being built.
/// </summary>
/// <remarks>
/// Run with "dotnet run --project src/Ghuri.Api -- seed-demo"; Program.cs
/// refuses it outside Development, so sample data can never reach a real
/// server. Safe to run again: a row whose slug already exists is skipped,
/// so it never duplicates and never overwrites an admin's edits.
/// No images - upload real photos through the admin screen.
/// </remarks>
internal sealed class DemoDataSeeder(AppDbContext db, ILogger<DemoDataSeeder> logger)
{
    private sealed record DemoCategory(string Name, string Icon);

    private sealed record DemoDestination(string CountryIso, string Name, string Summary, bool IsFeatured = false);

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
        new("BD", "Cox's Bazar", "The world's longest natural sea beach - 120 km of golden sand along the Bay of Bengal, with Himchari, Inani and Marine Drive.", true),
        new("BD", "Saint Martin's Island", "Bangladesh's only coral island: turquoise water, coconut palms, Chhera Dwip and fresh seafood on the beach.", true),
        new("BD", "Sylhet", "Tea gardens, the Ratargul swamp forest, Jaflong's stone-filled river and the clear water of Bholaganj Sada Pathor.", true),
        new("BD", "Sajek Valley", "The 'queen of hills' in Rangamati - wake up above a sea of clouds, with Konglak Para at sunset.", true),
        new("BD", "Sundarbans", "The largest mangrove forest on Earth and home of the Royal Bengal tiger, explored on a 3-day launch trip.", true),
        new("BD", "Bandarban", "Nilgiri, Nafakhum waterfall, Boga Lake and Keokradong - the highest hills and wildest trails in Bangladesh."),
        new("BD", "Srimangal", "The tea capital of Bangladesh: endless tea estates, Lawachara rainforest and the famous seven-layer tea."),
        new("BD", "Rangamati", "Kaptai Lake by boat, the hanging bridge, Shuvolong waterfall and the culture of the Chittagong Hill Tracts."),
        new("BD", "Khagrachari", "Alutila cave, Risang waterfall and quiet hill villages - an easy add-on to Sajek."),
        new("BD", "Kuakata", "'Daughter of the sea' - one of the few beaches where you watch both the sunrise and the sunset."),
        new("BD", "Tanguar Haor", "A vast wetland in Sunamganj, best explored on a houseboat beneath the hills of Meghalaya."),
        new("BD", "Nijhum Dwip", "A remote island of spotted deer, mangroves and empty beaches at the mouth of the Meghna."),
        new("BD", "Chattogram", "Patenga beach, Foy's Lake, Bayazid Bostami shrine and the gateway to the hill tracts."),
        new("BD", "Dhaka", "Old Dhaka's Lalbagh Fort, Ahsan Manzil, rickshaw rides on the Buriganga and legendary street food."),
        new("BD", "Sonargaon", "The ancient capital of Bengal - Panam City's abandoned mansions and the Folk Art Museum."),
        new("BD", "Bagerhat", "The Sixty Dome Mosque and Khan Jahan Ali's mosque city, a UNESCO World Heritage Site."),
        new("BD", "Paharpur", "Somapura Mahavihara, one of the largest Buddhist monasteries south of the Himalaya (UNESCO)."),

        // ---- International ----
        new("MV", "Maldives", "Overwater villas, house-reef snorkelling and powder-white beaches - the classic honeymoon.", true),
        new("TH", "Phuket", "Andaman beaches, Phi Phi and James Bond islands by speedboat, and Patong's nightlife.", true),
        new("ID", "Bali", "Temples, Ubud's rice terraces, Nusa Penida's cliffs and sunset beach clubs.", true),
        new("AE", "Dubai", "Burj Khalifa, desert safari with BBQ dinner, Dubai Mall and a dhow cruise on the Marina.", true),
        new("SA", "Makkah", "Masjid al-Haram and the Kaaba - the heart of every Umrah and Hajj journey.", true),
        new("SA", "Madinah", "Masjid an-Nabawi, the Prophet's Mosque, with ziyarah to Quba and Mount Uhud."),
        new("NP", "Pokhara", "Phewa Lake, paragliding from Sarangkot and sunrise over the Annapurna range.", true),
        new("NP", "Kathmandu", "Durbar squares, Swayambhunath, Boudhanath stupa and the gateway to the Himalaya."),
        new("BT", "Thimphu and Paro", "Tiger's Nest monastery, Punakha Dzong and Dochula Pass in the Land of the Thunder Dragon.", true),
        new("IN", "Kashmir", "Shikara rides on Dal Lake, Gulmarg's snow, Pahalgam's valleys and houseboat stays in Srinagar.", true),
        new("IN", "Darjeeling", "The toy train, sunrise over Kanchenjunga from Tiger Hill and some of the world's finest tea."),
        new("IN", "Sikkim", "Gangtok, Tsomgo Lake, Nathula Pass and the flower valleys of North Sikkim."),
        new("IN", "Goa", "Beaches, Portuguese churches, spice farms and famous beach shacks."),
        new("IN", "Rajasthan", "The palaces and forts of Jaipur, Udaipur's lakes and camel rides in Jaisalmer's desert."),
        new("IN", "Delhi and Agra", "The Taj Mahal at sunrise, Agra Fort, Qutub Minar and the lanes of Old Delhi."),
        new("TH", "Bangkok", "The Grand Palace, floating markets, rooftop views and endless street food."),
        new("TH", "Pattaya", "Coral Island, Nong Nooch garden and an easy beach escape from Bangkok."),
        new("MY", "Kuala Lumpur", "Petronas Towers, Batu Caves, Genting Highlands and great-value shopping."),
        new("MY", "Langkawi", "Duty-free island of the SkyBridge, mangrove boat tours and quiet beaches."),
        new("SG", "Singapore", "Marina Bay Sands, Gardens by the Bay, Sentosa and Universal Studios."),
        new("LK", "Kandy and Nuwara Eliya", "The Temple of the Tooth, the scenic hill-country train and misty tea estates."),
        new("TR", "Istanbul", "Hagia Sophia, the Blue Mosque, the Grand Bazaar and a Bosphorus cruise between two continents."),
        new("TR", "Cappadocia", "Sunrise hot-air balloon rides over fairy chimneys and nights in a cave hotel."),
        new("VN", "Ha Long Bay", "Thousands of limestone islands in emerald water - best seen on an overnight cruise."),
        new("EG", "Cairo", "The Pyramids of Giza, the Sphinx, the Egyptian Museum and a dinner cruise on the Nile."),
        new("JP", "Tokyo and Kyoto", "Neon Shibuya, Mount Fuji day trips, Kyoto's temples and cherry blossom season."),
        new("KR", "Seoul", "Gyeongbokgung Palace, Nami Island, K-culture streets and autumn colours."),
        new("CH", "Interlaken", "Alpine lakes, Jungfraujoch - the 'Top of Europe' - and Swiss mountain trains."),
        new("FR", "Paris", "The Eiffel Tower, the Louvre, Montmartre and cafés along the Seine."),
        new("AU", "Sydney", "The Opera House, Harbour Bridge climb, Bondi Beach and the Blue Mountains."),
        new("AZ", "Baku", "The Flame Towers, the old walled city and the mud volcanoes of Gobustan."),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            throw new InvalidOperationException(
                "The database has pending migrations. Run \".\\ef.cmd database update\" first, then seed.");

        var countries = await db.Countries.ToDictionaryAsync(c => c.IsoCode, c => c.Id, cancellationToken);
        if (countries.Count == 0)
            throw new InvalidOperationException(
                "No countries found - run \"dotnet run --project src/Ghuri.Api -- seed\" first (it adds the countries).");

        var addedCategories = await AddCategoriesAsync(cancellationToken);
        var addedDestinations = await AddDestinationsAsync(countries, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Demo data: added {Categories} categories and {Destinations} destinations (existing slugs were skipped).",
            addedCategories, addedDestinations);
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

    private async Task<int> AddDestinationsAsync(Dictionary<string, short> countries, CancellationToken cancellationToken)
    {
        var existing = (await db.Destinations.Select(d => d.Slug).ToListAsync(cancellationToken)).ToHashSet();
        var added = 0;

        for (var i = 0; i < Destinations.Length; i++)
        {
            var demo = Destinations[i];
            var slug = Slug.Create(demo.Name);
            if (existing.Contains(slug))
                continue;

            db.Destinations.Add(Destination.Create(
                countries[demo.CountryIso], demo.Name, slug, demo.Summary,
                isFeatured: demo.IsFeatured, sortOrder: (i + 1) * 10,
                seoTitle: $"{demo.Name} Tour Packages | Ghuri",
                seoDescription: demo.Summary.Length <= 160 ? demo.Summary : demo.Summary[..157] + "..."));
            added++;
        }

        return added;
    }
}
