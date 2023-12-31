using HtmlAgilityPack;
using System.Text.Json;

class Program
{
    private const string StartUrl = "https://itch.io/games/tag-furry";
    private static readonly HttpClient client = new HttpClient();
    private static List<Game> games = new List<Game>();

    public static async Task Main()
    {
        HtmlDocument html = new HtmlDocument();

        try
        {
            html.LoadHtml(await client.GetStringAsync(StartUrl));
            await GetSearchData(html);

            foreach (var game in games)
            {
                html.LoadHtml(await client.GetStringAsync(game.Link));
                await GetPageData(html, game.Name);
            }

            File.AppendAllText("result.json", JsonSerializer.Serialize(games));

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    public static async Task GetSearchData(HtmlDocument html)
    {
        HtmlNodeCollection nodes = html.DocumentNode.SelectNodes("//div[contains(@class,'game_cell_data')]");

        if (nodes != null)
        {
            foreach (HtmlNode node in nodes)
            {
                Game game = new Game();

                var link_and_name = node.SelectSingleNode(".//div[@class='game_title']/a");

                game.Link = link_and_name.GetAttributeValue("href", "");
                game.Name = link_and_name.InnerText;
                game.Genre = node.SelectSingleNode("//div[@class='game_genre']").InnerText;
                game.Developer = node.SelectSingleNode(".//div[@class='game_author']/a").InnerText;

                var platform_spans = node.SelectNodes(".//div[@class='game_platform']/span");
                List<string> platforms = platform_spans?.Select(span => span.GetAttributeValue("title", "").Split(" ").Last()).ToList() ?? new List<string>();

                game.Platforms = platforms;

                games.Add(game);
            }
        }
    }

    public static async Task GetPageData(HtmlDocument html, string name)
    {
        int index = games.FindIndex(game => game.Name == name);

        List<string> screenshots = new List<string>();
        var screenshot_nodes = html.DocumentNode.SelectNodes("//div[contains(@class,'screenshot_list')]/a");

        if (screenshot_nodes != null)
        {
            screenshots.AddRange(screenshot_nodes.Select(node => node.GetAttributeValue("href", "")));
        }

        var tag_nodes = html.DocumentNode.SelectNodes("//tr[td/text()[contains(.,'Tags')]]/td/a");
        games[index].Tags = tag_nodes?.Select(tag => tag.InnerText).ToList() ?? new List<string>();
        games[index].Images = screenshots;
        games[index].DevStatus = html.DocumentNode.SelectSingleNode("//tr[td/text()[contains(.,'Status')]]/td/a")?.InnerText ?? "";
        games[index].Date = html.DocumentNode.SelectSingleNode("//tr[td/text()[contains(.,'Release date')]]/td/abbr")?.GetAttributeValue("title", "") ?? "";
        games[index].Annotation = JsonSerializer.Serialize(html.DocumentNode.SelectSingleNode("//div[contains(@class,'formatted_description')]")?.OuterHtml ?? "");
    }
}

public class Game
{
    public string Name { get; set; }
    public string Date { get; set; }
    public string DevStatus { get; set; }
    public List<string> Platforms { get; set; }
    public string Genre { get; set; }
    public string Annotation { get; set; }
    public List<string> Tags { get; set; }
    public string Developer { get; set; }
    public string Link { get; set; }
    public List<string> Images { get; set; }
}