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
/// Every destination gets a gallery of four real photos from Wikimedia
/// Commons - a cover plus one per famous sight - downloaded at seed time
/// (DemoPhotoWriter; offline, drawn postcards stand in). A demo destination
/// that is already there with no photos, or only postcards, gets real ones
/// on the next run; one with any real photo - e.g. uploaded in the admin -
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

    /// <summary>The colours of a drawn postcard, when the real photo can't be downloaded: sky at the top, then the horizon.</summary>
    private sealed record Look(string SkyTop, string SkyBottom);

    /// <summary>A famous sight and its photo - a Wikimedia Commons file name, exactly as on commons.wikimedia.org.</summary>
    private sealed record Sight(string Name, string Photo);

    /// <summary>Cover = the Commons photo shown first; Sights = three more, one photo each.</summary>
    private sealed record DemoDestination(
        string CountryIso, string Name, Look Look, string Cover, Sight[] Sights, string Summary, bool IsFeatured = false);

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
        new("BD", "Cox's Bazar", Sea, "Cox's Bazar sea beach 01.jpg",
            [new("Himchari", "Himchari, Cox's Bazar (2Q7A1950).jpg"), new("Inani Beach", "Inani Beach (135005).jpg"), new("Marine Drive", "Cox's bazar marine drive.JPG")],
            "The world's longest natural sea beach - 120 km of golden sand along the Bay of Bengal, with Himchari, Inani and Marine Drive.", true),
        new("BD", "Saint Martin's Island", Coral, "Saint Martin (6).jpg",
            [new("Chhera Dwip", "Chhera Dwip, Cheradia Island, St. Martin's Island, Cox's Bazar, Bangladesh 3.jpg"), new("Coral Beach", "Coral Sea Beach of Chera Dwip, Saint Martin's Island (26629378198).jpg"), new("West Beach Sunset", "Sunset at Saint Martin's Island.JPG")],
            "Bangladesh's only coral island: turquoise water, coconut palms, Chhera Dwip and fresh seafood on the beach.", true),
        new("BD", "Sylhet", Tea, "Tea garden at Sripur (1).jpg",
            [new("Ratargul Swamp Forest", "Ratargul 785 retouched.jpg"), new("Jaflong", "Jaflong Sylhet.jpg"), new("Bholaganj Sada Pathor", "Sada Pathor, Bholaganj, Companyganj, Sylhet.jpg")],
            "Tea gardens, the Ratargul swamp forest, Jaflong's stone-filled river and the clear water of Bholaganj Sada Pathor.", true),
        new("BD", "Sajek Valley", Hills, "Clouds at Sajek Valley 20171220.jpg",
            [new("Konglak Para", "Konglak para, Sajek.jpg"), new("Hilltop Cottages", "Sajek Valley 20161205.jpg"), new("Valley View", "Sajek Valley, Rangamati,Bangladesh.jpg")],
            "The 'queen of hills' in Rangamati - wake up above a sea of clouds, with Konglak Para at sunset.", true),
        new("BD", "Sundarbans", Forest, "Sundarban Landscape during monsoon 17.jpg",
            [new("Kotka Beach", "Kotka sea beach.jpg"), new("Karamjal", "Karamjal Point-SUNDARBAN.jpg"), new("Hiron Point", "Excoecharia forest with sand dunes at Hiron Point in Sundarbans.jpg")],
            "The largest mangrove forest on Earth and home of the Royal Bengal tiger, explored on a 3-day launch trip.", true),
        new("BD", "Bandarban", Hills, "Bandarban natural view.jpg",
            [new("Nilgiri", "Nilgiri, Bandarban, Bangladesh.jpg"), new("Nafakhum Waterfall", "Nafakhum water falls.jpg"), new("Boga Lake", "Boga lake 02.jpg")],
            "Nilgiri, Nafakhum waterfall, Boga Lake and Keokradong - the highest hills and wildest trails in Bangladesh."),
        new("BD", "Srimangal", Tea, "Tea gardens Srimangal Sreemangal Upazila Moulvibazar Maulvibazar Moulavibazar Sylhet 01.jpg",
            [new("Tea Estates", "Zareen Tea Estate Sylhet Division - Bangladesh.jpg"), new("Lawachara Rainforest", "Lawachara Forest.jpg"), new("Seven-Layer Tea", "Seven Layers Tea (01).jpg")],
            "The tea capital of Bangladesh: endless tea estates, Lawachara rainforest and the famous seven-layer tea."),
        new("BD", "Rangamati", Lake, "Sunset at Rangamati 2018.jpg",
            [new("Kaptai Lake", "Kaptai lake view.jpg"), new("Hanging Bridge", "Hanging Bridge, Kaptai Lake.jpg"), new("Shuvolong Waterfall", "Shuvolong waterfalls.jpg")],
            "Kaptai Lake by boat, the hanging bridge, Shuvolong waterfall and the culture of the Chittagong Hill Tracts."),
        new("BD", "Khagrachari", Hills, "Khagrachari district council park.JPG",
            [new("Alutila Cave", "Entrance, Alutila Cave (01).jpg"), new("Risang Waterfall", "Risang Waterfall, Khagrachhari.jpg"), new("Hill Trails", "Risang Path.jpg")],
            "Alutila cave, Risang waterfall and quiet hill villages - an easy add-on to Sajek."),
        new("BD", "Kuakata", Sea, "Kuakata Sea Beach.JPG",
            [new("Sunrise Beach", "Sunrise at Kuakata beach.JPG"), new("Sunset Point", "Kuakata sunset 2023.jpg"), new("Beach Life", "Kuakata Sea Beach.jpg")],
            "'Daughter of the sea' - one of the few beaches where you watch both the sunrise and the sunset."),
        new("BD", "Tanguar Haor", Lake, "Tanguar haor, Bangladesh 01.jpg",
            [new("Houseboat Stay", "Tanguar Haor Houseboat.jpg"), new("Swamp Trees", "Tanguar Haor (টাঙ্গুয়ার হাওর).JPG"), new("Sampan Ride", "Sampan in Tanguar Haor.jpg")],
            "A vast wetland in Sunamganj, best explored on a houseboat beneath the hills of Meghalaya."),
        new("BD", "Nijhum Dwip", Forest, "Nijhum Dwip.jpg",
            [new("Spotted Deer", "Spotted deer in Nijhum dweep national park( char osman bit), Noakhali.jpg"), new("Mangrove Forest", "Mangrove forest at nijhum dwip.jpg"), new("Empty Beaches", "Nijhum Dwip Beach-1.jpg")],
            "A remote island of spotted deer, mangroves and empty beaches at the mouth of the Meghna."),
        new("BD", "Chattogram", Sea, "Patenga sea beach 001.jpg",
            [new("Patenga Beach", "Side view of Patenga sea beach (10).jpg"), new("Foy's Lake", "Foy's Lake, Chattogram (08).jpg"), new("Bayazid Bostami Shrine", "Shrine of Bayazid Bostami 01.jpg")],
            "Patenga beach, Foy's Lake, Bayazid Bostami shrine and the gateway to the hill tracts."),
        new("BD", "Dhaka", City, "Skyline View of the City of Dhaka on a Cloudy Day.jpg",
            [new("Lalbagh Fort", "2. লালবাগের কেল্লা.jpg"), new("Ahsan Manzil", "Historic Ahsan Manzil Palace Bangladesh.jpg"), new("Buriganga River", "Small boats on the Buriganga River.jpg")],
            "Old Dhaka's Lalbagh Fort, Ahsan Manzil, rickshaw rides on the Buriganga and legendary street food."),
        new("BD", "Sonargaon", Heritage, "Sonargaon Folk Art and Craft Museum (31000427270).jpg",
            [new("Panam City", "Panam Nagar (7).jpg"), new("Folk Art Museum", "Bara Sardar Bari at Sonargaon Folk Art Museum IMG 20250209 143333.jpg"), new("Goaldi Mosque", "Goaldi mosque, sonargaon.jpg")],
            "The ancient capital of Bengal - Panam City's abandoned mansions and the Folk Art Museum."),
        new("BD", "Bagerhat", Heritage, "Sixty Dome Mosque at Bagerhat (7).jpg",
            [new("Sixty Dome Mosque", "Way of Sixty Dome Mosque.jpg"), new("Khan Jahan Ali's Tomb", "Tomb of Khan Jahan Ali (1).jpg"), new("Nine Dome Mosque", "Nine Dome Mosque, Bagerhat, Bangladesh.jpg")],
            "The Sixty Dome Mosque and Khan Jahan Ali's mosque city, a UNESCO World Heritage Site."),
        new("BD", "Paharpur", Heritage, "Aerial view of Somapura Mahavihara.jpg",
            [new("Somapura Mahavihara", "First level plinth at Somapura Mahavihara.jpg"), new("Terracotta Plaques", "Paharpur Terracotta by Farhana 2.jpg"), new("Site Museum", "Paharpur Museum 1.jpg")],
            "Somapura Mahavihara, one of the largest Buddhist monasteries south of the Himalaya (UNESCO)."),

        // ---- International ----
        new("MV", "Maldives", Coral, "Diamonds Thudufushi Beach and Water Villas, May 2017 -04.jpg",
            [new("Overwater Villas", "Diamonds Thudufushi Beach and Water Villas, May 2017 -09.jpg"), new("House-Reef Snorkelling", "Snorkeling in the Indian Ocean in the Maldives..JPG"), new("Sandbank Picnic", "Sandbank. Eriyadu, Maldives.jpg")],
            "Overwater villas, house-reef snorkelling and powder-white beaches - the classic honeymoon.", true),
        new("TH", "Phuket", Sea, "Banana beach Phuket 2017 - 02.jpg",
            [new("Phi Phi Islands", "Playa Maya, Ko Phi Phi, Tailandia, 2013-08-19, DD 13.JPG"), new("James Bond Island", "Khao Phing Kan and Koh Tapu (James Bond Island).jpg"), new("Patong Beach", "Long-Tail Boats on Patong Beach Pukhet.jpg")],
            "Andaman beaches, Phi Phi and James Bond islands by speedboat, and Patong's nightlife.", true),
        new("ID", "Bali", Forest, "Sunset, Kuta, Bali, Indonesia, 20220825 1755 0879.jpg",
            [new("Ubud Rice Terraces", "Tegallalang Rice Terraces Bali.jpg"), new("Nusa Penida", "Kelingking Beach (T-Rex Bay) of Nusa Penida, Bali (2025) - img 07.jpg"), new("Tanah Lot", "Tanah-Lot Bali Indonesia Pura-Tanah-Lot-01.jpg")],
            "Temples, Ubud's rice terraces, Nusa Penida's cliffs and sunset beach clubs.", true),
        new("AE", "Dubai", Desert, "Dubai Skyline 2016.jpg",
            [new("Burj Khalifa", "Burj Khalifa (16260269606).jpg"), new("Desert Safari", "Desert safari eve.jpg"), new("Dhow Cruise", "Dubai- Al Seef Dhow Cruise 41.jpg")],
            "Burj Khalifa, desert safari with BBQ dinner, Dubai Mall and a dhow cruise on the Marina.", true),
        new("SA", "Makkah", Holy, "Pilgrims begin this year's Hajj rituals with a circumambulation of the Kaaba (2025) (2).jpg",
            [new("Masjid al-Haram", "Masjid al-Haram, Tawaf 20092012 1130PM (8008466944).jpg"), new("The Kaaba", "Kaaba, Makkah3.jpg"), new("Jabal al-Nour", "Jabbal An-Nour (2024).jpg")],
            "Masjid al-Haram and the Kaaba - the heart of every Umrah and Hajj journey.", true),
        new("SA", "Madinah", Holy, "Al Masjid an Nabawi 3.jpg",
            [new("Masjid an-Nabawi", "Masjid Nabawi. Medina, Saudi Arabia.jpg"), new("Quba Mosque", "Quba Mosque Full Picture (2024).jpg"), new("Mount Uhud", "Jabal-e-Uhud.jpg")],
            "Masjid an-Nabawi, the Prophet's Mosque, with ziyarah to Quba and Mount Uhud."),
        new("NP", "Pokhara", Lake, "Pokhara, View of Pokhara Valley, Nepal.jpg",
            [new("Phewa Lake", "Phewa Lake of Pokhara city.jpg"), new("Sarangkot", "Sarangkot, Nepal-WLV-1691.jpg"), new("Annapurna Sunrise", "Sunrise Annapurna Pokhara Nepal Feb13 DSC 1583.jpg")],
            "Phewa Lake, paragliding from Sarangkot and sunrise over the Annapurna range.", true),
        new("NP", "Kathmandu", Heritage, "View of Kathmandu Valley-070A1871.jpg",
            [new("Durbar Square", "Kathmandu Durbar Square, Shiva Parvati Temple, Nepal (edit).jpg"), new("Swayambhunath", "Swayambhu, Kathmandu, Nepal.jpg"), new("Boudhanath Stupa", "Boudha Stupa 2018 04.jpg")],
            "Durbar squares, Swayambhunath, Boudhanath stupa and the gateway to the Himalaya."),
        new("BT", "Thimphu and Paro", Hills, "A view of Thimphu from Talakha Gonpa.jpg",
            [new("Tiger's Nest", "Paro Taktsang, Bhutan (edited).jpg"), new("Punakha Dzong", "Punakha Dzong, Bhutan 02.jpg"), new("Dochula Pass", "Dochula Pass September 2023.jpg")],
            "Tiger's Nest monastery, Punakha Dzong and Dochula Pass in the Land of the Thunder Dragon.", true),
        new("IN", "Kashmir", Snow, "Krishansar Lake, Sonmarg, Kashmir valley, India 01.jpg",
            [new("Dal Lake Shikara", "Empty shikara on Dal Lake, Srinagar, India 2013-08-23 (flickr 9967093983).jpg"), new("Gulmarg", "Gulmarg 2.jpg"), new("Pahalgam", "Pahalgam Valley.jpg")],
            "Shikara rides on Dal Lake, Gulmarg's snow, Pahalgam's valleys and houseboat stays in Srinagar.", true),
        new("IN", "Darjeeling", Tea, "Darjeeling, India, Tea plantations on hills.jpg",
            [new("Toy Train", "Darjeeling Himalayan Railway,toy train (1).jpg"), new("Tiger Hill", "Kanchenjunga range from Tiger Hill, Darjeeling.jpg"), new("Tea Gardens", "PXL 20240323 065728783 Cloud and Tea Garden at Namring Tea Garden Darjeeling West Bengal 734226 04.jpg")],
            "The toy train, sunrise over Kanchenjunga from Tiger Hill and some of the world's finest tea."),
        new("IN", "Sikkim", Snow, "Gurudongmar Lake Sikkim, India (edit).jpg",
            [new("Gangtok", "View of Gangtok city from Ropeway.jpg"), new("Tsomgo Lake", "Tsomgo Lake in October month.jpg"), new("Nathula Pass", "Nathula Pass in October.jpg")],
            "Gangtok, Tsomgo Lake, Nathula Pass and the flower valleys of North Sikkim."),
        new("IN", "Goa", Sea, "Cola Beach Bay South Goa Jan19 DSC06186.jpg",
            [new("Baga Beach", "Goa Beach - Baga Beach.jpg"), new("Old Goa Churches", "Basilica of Bom Jesus Church ,Old GOA.jpg"), new("Spice Farms", "Spice Plantation in Goa.JPG")],
            "Beaches, Portuguese churches, spice farms and famous beach shacks."),
        new("IN", "Rajasthan", Desert, "Hawa Mahal in Jaipur India.jpg",
            [new("Amber Fort", "Amber Fort Jaipur 01.jpg"), new("Udaipur Lakes", "Lake Palace, Lake Pichola, Udaipur.jpg"), new("Jaisalmer Desert", "Camel rides in Jaisalmer, Thar Desert 01.jpg")],
            "The palaces and forts of Jaipur, Udaipur's lakes and camel rides in Jaisalmer's desert."),
        new("IN", "Delhi and Agra", Heritage, "Taj Mahal, Agra, India edit2.jpg",
            [new("Taj Mahal", "Aks The Reflection Taj Mahal.jpg"), new("Agra Fort", "20191204 Moat and walls of Agra Fort 0925 6582.jpg"), new("Qutub Minar", "View of Qutub Minar (1).jpg")],
            "The Taj Mahal at sunrise, Agra Fort, Qutub Minar and the lanes of Old Delhi."),
        new("TH", "Bangkok", City, "Bangkok skyline 2018.jpg",
            [new("Grand Palace", "Grand Palace, Bangkok 4.jpg"), new("Floating Market", "Damnoen Saduak Floating Market, Damnoen Saduak, Thailand (Unsplash).jpg"), new("Rooftop Views", "View over Sukhumvit from a rooftop, Bangkok.jpg")],
            "The Grand Palace, floating markets, rooftop views and endless street food."),
        new("TH", "Pattaya", Sea, "Pattaya Beach, Trees, Thailand.jpg",
            [new("Coral Island", "Koh Larn Island from the Bird and Bees resort by Don Ramey Logan.jpg"), new("Nong Nooch Garden", "Overview Nong Nooch Botanical Garden.jpg"), new("Walking Street", "Pattaya, Walking Street at night, Thailand.jpg")],
            "Coral Island, Nong Nooch garden and an easy beach escape from Bangkok."),
        new("MY", "Kuala Lumpur", City, "Kuala Lumpur Malaysia Skyline-03.jpg",
            [new("Petronas Towers", "2016 Kuala Lumpur, Petronas Towers (27).jpg"), new("Batu Caves", "Batu Caves stairs 2022-05.jpg"), new("Genting Highlands", "The Beauty of Genting Highlands.jpg")],
            "Petronas Towers, Batu Caves, Genting Highlands and great-value shopping."),
        new("MY", "Langkawi", Coral, "Langkawi 003.jpg",
            [new("SkyBridge", "Langkawi Sky Bridge (November 2016).jpg"), new("Mangrove Tour", "Langkawi Mangrove Forest.jpg"), new("Cenang Beach", "Cenang Beach view, Langkawi.jpg")],
            "Duty-free island of the SkyBridge, mangrove boat tours and quiet beaches."),
        new("SG", "Singapore", City, "Skylines of the Central Business District, Singapore at dusk.jpg",
            [new("Marina Bay Sands", "Marina Bay Sands and illuminated polyhedral building Louis Vuitton over the water at blue hour with pink clouds in Singapore.jpg"), new("Gardens by the Bay", "Supertree Grove, Gardens by the Bay, Singapore1.jpg"), new("Sentosa", "Palawan Beach, Sentosa (184817).jpg")],
            "Marina Bay Sands, Gardens by the Bay, Sentosa and Universal Studios."),
        new("LK", "Kandy and Nuwara Eliya", Tea, "Island of Kandy lake.JPG",
            [new("Temple of the Tooth", "Sri Lanka, Kandy, Temple of the Tooth, Sri Dalada Maligawa.jpg"), new("Hill-Country Train", "A Train Crosses The Nine Arches Bridge (243270425).jpeg"), new("Tea Estates", "Sri Lanka, Tea plantations near Nuwara Eliya, Tea estate.jpg")],
            "The Temple of the Tooth, the scenic hill-country train and misty tea estates."),
        new("TR", "Istanbul", Heritage, "Istanbul skyline 02.jpg",
            [new("Hagia Sophia", "Hagia Sophia Mars 2013.jpg"), new("Blue Mosque", "Exterior of Sultan Ahmed I Mosque in Istanbul, Turkey 002.jpg"), new("Bosphorus Cruise", "Sultanahmet ferry on the Bosphorus in Istanbul, Turkey 001.jpg")],
            "Hagia Sophia, the Blue Mosque, the Grand Bazaar and a Bosphorus cruise between two continents."),
        new("TR", "Cappadocia", Desert, "Cappadocia Aerial View Landscape.jpg",
            [new("Hot-Air Balloons", "Hot air balloon in Cappadocia 02.jpg"), new("Fairy Chimneys", "Fairy chimneys in Cappadocia.JPG"), new("Cave Hotels", "Local Cave Hotels in Cappadocia, Turkiye.jpg")],
            "Sunrise hot-air balloon rides over fairy chimneys and nights in a cave hotel."),
        new("VN", "Ha Long Bay", Coral, "View of sea from Titov Island, Ha Long Bay, Vietnam, 20240128 1337 3732.jpg",
            [new("Limestone Islands", "Vietnam, Ha Long Bay, Island.jpg"), new("Overnight Cruise", "A line up of cruise boats on the Ha Long Bay (31598856166).jpg"), new("Sung Sot Cave", "Sung Sot Cave, Ha Long Bay, Vietnam, 20240128 1607 3880.jpg")],
            "Thousands of limestone islands in emerald water - best seen on an overnight cruise."),
        new("EG", "Cairo", Desert, "Cairo skyline, Nile River, Egypt.jpg",
            [new("Pyramids of Giza", "All Gizah Pyramids.jpg"), new("The Sphinx", "Great Sphinx of Giza (أبو الهول).jpg"), new("Felucca on the Nile", "FELUCCA NILE TRIP (2).jpg")],
            "The Pyramids of Giza, the Sphinx, the Egyptian Museum and a dinner cruise on the Nile."),
        new("JP", "Tokyo and Kyoto", City, "Minato City, Tokyo, Japan (Night).jpg",
            [new("Shibuya Crossing", "Tokyo Shibuya Scramble Crossing 2018-10-09.jpg"), new("Mount Fuji", "Mount Fuji at sunset, March 2025.jpg"), new("Fushimi Inari", "Double torii path at Fushimi Inari Taisha Shrine, Kyoto, Japan.jpg")],
            "Neon Shibuya, Mount Fuji day trips, Kyoto's temples and cherry blossom season."),
        new("KR", "Seoul", City, "Han River Seoul skyline Pixabay 1214950.jpg",
            [new("Gyeongbokgung Palace", "Gyeonghoeru (Royal Banquet Hall) at Gyeongbokgung Palace, Seoul.jpg"), new("Bukchon Hanok Village", "Bukchon-ro 11-gil street with hanok houses at blue hour in Bukchon Hanok Village Seoul.jpg"), new("Myeongdong", "Myeongdong at night.jpg")],
            "Gyeongbokgung Palace, Nami Island, K-culture streets and autumn colours."),
        new("CH", "Interlaken", Snow, "Interlaken view from Harderkulm (Ank Kumar) 05.jpg",
            [new("Jungfraujoch", "Jungfraujoch, Swiss Alps( Ank Kumar , Infosys Limited ) 11.jpg"), new("Lake Brienz", "2025-08-22 - Lake Brienz - 01.jpg"), new("Swiss Mountain Train", "00 1426 Wengernalpbahn - Berner Oberland (Schweiz).jpg")],
            "Alpine lakes, Jungfraujoch - the 'Top of Europe' - and Swiss mountain trains."),
        new("FR", "Paris", City, "Eiffel Tower and Pont Alexandre III at night.jpg",
            [new("Eiffel Tower", "Paris - The Eiffel Tower in spring - 2307.jpg"), new("The Louvre", "Louvre (Ank kumar Infosys) 05.jpg"), new("Montmartre", "Paris, Sacré-Cœur de Montmartre -- 2014 -- 1193.jpg")],
            "The Eiffel Tower, the Louvre, Montmartre and cafés along the Seine."),
        new("AU", "Sydney", Sea, "Sydney Opera House and Harbour Bridge Dusk (2) 2019-06-21.jpg",
            [new("Opera House", "Sydney Opera House - Dec 2008.jpg"), new("Harbour Bridge", "Sydney Harbour Bridge from Circular Quay.jpg"), new("Bondi Beach", "Sydney (AU), Bondi Beach -- 2019 -- 2349.jpg")],
            "The Opera House, Harbour Bridge climb, Bondi Beach and the Blue Mountains."),
        new("AZ", "Baku", City, "Baku Skyline qiz qalasi.jpg",
            [new("Flame Towers", "Flame towers from Baku boulevard.JPG"), new("Old City", "Bakı şəhəri,İçəri şəhər, qoşa qala divarları.jpg"), new("Gobustan Mud Volcanoes", "Gobustan mud volcanoes 08.jpg")],
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
        var (addedDestinations, galleries) = await AddDestinationsAsync(countries, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // After the save above: packages need the destinations and categories to exist.
        var addedPackages = await packageSeeder.SeedAsync(cancellationToken);

        logger.LogInformation(
            "Demo data: added {Categories} categories, {Destinations} destinations, photo galleries for {Galleries} destinations "
            + "and {Packages} published packages (existing slugs were skipped).",
            addedCategories, addedDestinations, galleries, addedPackages);
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

    /// <returns>How many destinations were added, and how many got a new photo gallery (new ones + old ones upgraded).</returns>
    private async Task<(int Added, int Galleries)> AddDestinationsAsync(
        Dictionary<string, Country> countries, CancellationToken cancellationToken)
    {
        // Images included: an existing destination with no photos, or only postcards, gets real ones.
        var existing = await db.Destinations.Include(d => d.Images).ToDictionaryAsync(d => d.Slug, cancellationToken);
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var added = 0;
        var galleries = 0;

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
                var current = destination.Images.Select(image => image.FileId).ToList();
                if (current.Count > 0 && !await photos.AllPostcardsAsync(current, cancellationToken))
                    continue; // has real photos already - never replace an admin's

                // Postcards are only drawn for an empty gallery; replacing postcards with postcards helps no one.
                var fileIds = await GetPhotosAsync(demo, country.Name, allowPostcards: current.Count == 0, nowUtc, cancellationToken);
                if (fileIds.Count == 0)
                    continue; // still offline - keep the postcards it has

                destination.SetImages(fileIds);
                await photos.ForgetAsync(current, cancellationToken);
            }
            else
            {
                destination = Destination.Create(
                    country.Id, demo.Name, slug, demo.Summary,
                    isFeatured: demo.IsFeatured, sortOrder: (i + 1) * 10,
                    seoTitle: $"{demo.Name} Tour Packages | Ghuri",
                    seoDescription: demo.Summary.Length <= 160 ? demo.Summary : demo.Summary[..157] + "...");
                destination.SetImages(await GetPhotosAsync(demo, country.Name, allowPostcards: true, nowUtc, cancellationToken));
                db.Destinations.Add(destination);
                added++;
            }

            galleries++;
        }

        return (added, galleries);
    }

    /// <summary>The cover, then one photo per sight - four in all.</summary>
    private Task<List<Guid>> GetPhotosAsync(
        DemoDestination demo, string countryName, bool allowPostcards, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var place = $"{demo.Name}, {countryName}";
        List<DemoPhotoWriter.Shot> shots =
        [
            new(demo.Cover, demo.Name, countryName),
            .. demo.Sights.Select(sight => new DemoPhotoWriter.Shot(sight.Photo, sight.Name, place)),
        ];
        return photos.SaveAsync(demo.Name, shots, demo.Look.SkyTop, demo.Look.SkyBottom, allowPostcards, nowUtc, cancellationToken);
    }
}
