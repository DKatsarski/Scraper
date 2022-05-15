// See https://aka.ms/new-console-template for more information
using Dnevnik;
using Dnevnik.Models;
using Dnevnik.Persistence;
using HtmlAgilityPack;
using NLog;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

//get all the dates for a period of time
var startDate = DateTime.Parse("04/22/2022");
var endDate = DateTime.Now;
var allDatesFormatted = EachDay(startDate, endDate);
var listOfAllDates = new Stack<string>(allDatesFormatted);
var httpClient = new HttpClient();
var httpDocument = new HtmlDocument();
var articles = new List<Article>();
var comments = new List<Comment>();
Logger log = LogManager.GetCurrentClassLogger();
var context = new DnevnikContext();
const string DateFormat = "dd/MM/yyyy";
var cultureInfo = CultureInfo.InvariantCulture;

static IEnumerable<string> EachDay(DateTime from, DateTime thru)
{
    for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
        yield return day.ToString("yyyy/MM/dd");
}



while (listOfAllDates.Any())
{
    var date = listOfAllDates.Pop();
    log.Info("The Current date is {0}", date);
    var linksOfTheDay = new List<string>(await TakeAllLinksOfDay(date));

    await ScarapeDay(linksOfTheDay);


}

async Task ScarapeDay(List<string> linksOfTheDay)
{
    var articleLink = string.Empty;
    var commentsLink = string.Empty;

    foreach (var link in linksOfTheDay)
    {
        var idForegin = 0;

        var tempStr = string.Empty;

        if (link == articleLink || link == commentsLink)
        {
            continue;
        }

        if (link.Contains("/comments"))
        {
            tempStr = link.Substring(0, link.IndexOf("/comments"));
             articleLink = listOfAllDates.Where(x => x == tempStr).FirstOrDefault();
             commentsLink = link;
            //TODO: scrape acticle and then comments

     
        }
        else
        {
            articleLink = link;
            commentsLink = linksOfTheDay.Where(x => x == link + "comments").FirstOrDefault();
            //TODO: scrape acticle and then comments

        }


        //if (link.Contains("/comments"))
        //{
        //    //var subStr = link.Substring(0, link.IndexOf("/comments"));
        //    //var articleLink = listOfAllDates.Where(x => x == subStr).FirstOrDefault();
        //    //var commentsLink = link;

        //    articles = await ScrapeArticle(httpClient, httpDocument, articleLink);
        //    foreach (var article in articles)
        //    {
        //        var currentArticle = await context.AddAsync(article);

        //        await context.SaveChangesAsync();
        //        idForegin = currentArticle.Entity.Id;
        //    }

        //    comments = await ScrapeComments(httpClient, httpDocument, commentsLink, idForegin);
        //    foreach (var comment in comments)
        //    {
        //        //TODO: filter by comment, take substring, scraep article first, take ID, and then scrape comments with the ID of articl e
        //        var currentComment = await context.AddAsync(comment);
        //        await context.SaveChangesAsync();
        //        idForegin = currentComment.Entity.Id;
        //    }
            

        //}
        //else
        //{
        //    articles = await ScrapeArticle(httpClient, httpDocument, link);
        //    foreach (var article in articles)
        //    {
        //        // add to db
        //       var currentArticle = await context.AddAsync(article);
                
        //       await context.SaveChangesAsync();
        //        idForegin = currentArticle.Entity.Id;
        //    }

   
        //}
    }
}

async Task<List<Comment>> ScrapeComments(HttpClient httpClient, HtmlDocument httpDocument, string commentsOfCurrentArticle, int foreignKey)
{
    return null;
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
            .InnerText.Replace("&quot;", "'");


        

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
            sb.AppendLine(node.InnerText);
        }

        var resultString = Regex.Replace(sb.ToString().Trim(), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

        articles.Add(new Article
        {
            Title = title,
            Content = resultString.Replace("&quot;", "'"),
            ArticleLink = link,
            DateModified = DateTime.Parse(dateModified).Date,
            DatePublished = DateTime.Parse(datePublished).Date
        });

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





