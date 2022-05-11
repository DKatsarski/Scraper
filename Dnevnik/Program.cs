// See https://aka.ms/new-console-template for more information
using Dnevnik;
using Dnevnik.Persistence;
using HtmlAgilityPack;
using NLog;
using System.Text;
using System.Text.RegularExpressions;

//get all the dates for a period of time
var startDate = DateTime.Parse("04/22/2022");
var endDate = DateTime.Now;
var allDatesFormatted = EachDay(startDate, endDate);
var listOfAllDates = new Queue<string>(allDatesFormatted);
var httpClient = new HttpClient();
var httpDocument = new HtmlDocument();
var articles = new List<Article>();
Logger log = LogManager.GetCurrentClassLogger();
var context = new DnevnikContext();


static IEnumerable<string> EachDay(DateTime from, DateTime thru)
{
    for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
        yield return day.ToString("yyyy/MM/dd");
}



while (listOfAllDates.Any())
{
    var date = listOfAllDates.Dequeue();
    log.Info("The CUrrent date is {0}", date);
    var linksOfTheDay = new Queue<string>(await TakeAllLinksOfDay(date));
    var articleLink = linksOfTheDay.Dequeue();
    var commentsOfCurrentArticle = linksOfTheDay.Where(x => x.Contains(articleLink)).FirstOrDefault();

    Thread.Sleep(2000);
    articles = await ScrapeArticle(httpClient, httpDocument, articleLink);

    if (commentsOfCurrentArticle != null)
    {
        Thread.Sleep(2000);
        await ScrapeComments(httpClient, httpDocument, commentsOfCurrentArticle);
    }



}

async Task ScrapeComments(HttpClient httpClient, HtmlDocument httpDocument, string commentsOfCurrentArticle)
{

}

async Task<List<Article>> ScrapeArticle(HttpClient httpClient, HtmlDocument htmlDocument, string link)
{
    var articles = new List<Article>();
    var sb = new StringBuilder();
    var html = await httpClient.GetStringAsync(link);
    htmlDocument.LoadHtml(html);

    var divs =
    htmlDocument
    .DocumentNode
    .Descendants("article")
    .Where(node => node.GetAttributeValue("class", "")
    .Equals("general-article-v2 article"))
    .ToList();

    foreach (var div in divs)
    {
        var title = div
            .Descendants("h1")
            .FirstOrDefault()?
            .InnerText;




        var content = div
        .SelectNodes("//div[@class='article-content']");

        var datePublished = div
            .Descendants("time")
            .Where(node => node.GetAttributeValue("itemprop", "")
            .Equals("datePublished"))
            .FirstOrDefault()?
            .Attributes["content"].Value;
            //.InnerText;

        var dateModified = div
            .Descendants("meta")
            .Where(node => node.GetAttributeValue("itemprop", "")
            .Equals("dateModified"))
            .FirstOrDefault()?
            .Attributes["content"].Value;

        foreach (var node in content)
        {
            sb.AppendLine(String.Format("{0}, {1}, {2}", "2022/04/06", title, node.InnerText));
        }

        articles.Add(new Article
        {
            Title = title,
            Content = sb.ToString(),
            DateModified = DateTime.Parse(dateModified),
            DatePublished = DateTime.Parse(datePublished)
        });

        await context.AddAsync(new Article
        {
            Title = title,
            Content = sb.ToString(),
            DateModified = DateTime.Parse(dateModified),
            DatePublished = DateTime.Parse(datePublished)
        });

       await context.SaveChangesAsync();

        //var filePath = @"C:\Users\\dkats\Desktop\asdff.csv";

        //var resultString = Regex.Replace(sb.ToString().Trim(), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
        //await File.AppendAllTextAsync(filePath, resultString, Encoding.UTF8);

    }
    return articles;


}

var articlesss = await startCrawlerasync();

/// <summary>
/// article-content - class
/// keywords - id

/// </summary>

static async Task<List<Article>> startCrawlerasync()
{
    var url = "https://www.dnevnik.bg/allnews/2022/04/25/";
    var httpClient = new HttpClient();
    var html = await httpClient.GetStringAsync(url);
    var htmlDocument = new HtmlDocument();
    htmlDocument.LoadHtml(html);
    var articles = new List<Article>();
    var list = new HashSet<string>();

    var divs =
        htmlDocument
        .DocumentNode
        .Descendants("div")
        .Where(node => node.GetAttributeValue("class", "")
        .Equals("grid-container"))
        .ToList();


    foreach (HtmlNode div in divs)
    {
        var article = new Article()
        {
            Content = div.Descendants("article").FirstOrDefault()?.InnerText
        };

        var a = div.Descendants("a")
            .Select(node => node
            .GetAttributeValue("href", String.Empty)).ToList();

        list = div
           .Descendants("a")
           .Select(node => node
           .GetAttributeValue("href", String.Empty))
           .Where(x => x.StartsWith("http"))
           .ToHashSet();


        articles.Add(article);
    }

    return articles;
}

static async Task<HashSet<string>> TakeAllLinksOfDay(string dataInString)
{
    var url = "https://www.dnevnik.bg/allnews/" + dataInString;
    var httpClient = new HttpClient();
    var html = await httpClient.GetStringAsync(url);
    var htmlDocument = new HtmlDocument();
    htmlDocument.LoadHtml(html);
    var listLinks = new HashSet<string>();

    var divs =
        htmlDocument
        .DocumentNode
        .Descendants("div")
        .Where(node => node.GetAttributeValue("class", "")
        .Equals("grid-container"))
        .ToList();


    foreach (HtmlNode div in divs)
    {

        var a = div.Descendants("a")
            .Select(node => node
            .GetAttributeValue("href", String.Empty)).ToList();

        listLinks = div
           .Descendants("a")
           .Select(node => node
           .GetAttributeValue("href", String.Empty))
           .Where(x => x.StartsWith("http"))
           .ToHashSet();
    }

    return listLinks;
}




static Func<int, HashSet<string>> FilterLinks(Func<int, HashSet<string>> links = null)
{

    return links;
};





