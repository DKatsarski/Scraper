using Dnevnik;
using Dnevnik.Models;
using Dnevnik.Persistence;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NLog;
using System.Text;
using System.Text.RegularExpressions;

//get all the dates for a period of time

Console.WriteLine("Please provide the start date of the period you want to scrape articles from");
Console.WriteLine("Format is in MM/DD/YYYY");
Console.WriteLine("Type here: ");
var startDateParse = Console.ReadLine();
var startDate = DateTime.Parse(startDateParse);
Console.WriteLine("Please provide the end date of the period you want to scrape articles from");
Console.WriteLine("Format is in MM/DD/YYYY");
Console.WriteLine("Type here: ");
var endDateParse = Console.ReadLine();
var endDate = DateTime.Parse(endDateParse);
//TODO: Add validations for the formats
//var endDate = DateTime.Now;
//var endDate = DateTime.Parse("07/14/2021");
var allDatesFormatted = EachDay(startDate, endDate);

var listOfAllDates = new Stack<string>(allDatesFormatted);
var htmlDocument = new HtmlDocument();
var articles = new List<Article>();
var comments = new List<Comment>();
var random = new Random();

Logger log = LogManager.GetCurrentClassLogger();
var context = new DnevnikContext();

//for faster performance
context.ChangeTracker.AutoDetectChangesEnabled = false;

await ScrapeAll(listOfAllDates, htmlDocument);

async Task ScrapeAll(Stack<string> listOfAllDates, HtmlDocument htmlDocument)
{
    while (listOfAllDates.Any())
    {
        if (listOfAllDates.Count() == 1)
        {
            log.Info("Last day of the input data");
        }

        var date = listOfAllDates.Pop();
        log.Info("The date about to be scrapted is {0}", date);
        var linksOfADay = new List<string>();
        try
        {
            linksOfADay = new List<string>(await TakeAllLinksOfDay(htmlDocument, date));
            //linksOfADay.Add("https://www.dnevnik.bg/razvlechenie/2017/05/17/2973119_komiks_na_denia_-_17_mai/");
        }
        catch (Exception ex)
        {

            Thread.Sleep(30000);
            if (ex.Message.Contains("503"))
            {
                log.Error("503 error occured! in TakeAllLinksOfDay");
                await ScrapeAll(listOfAllDates, htmlDocument);
            }
            log.Error("This Error occured: {0}", ex.Message);
            throw ex;
        }

        if (linksOfADay.Count() == 0)
        {
            log.Info("No more link of this day {0}", date);
            continue;
        }

        try
        {
            await ScarapeDay(linksOfADay);
        }
        catch (Exception ex)
        {
            Thread.Sleep(30000);
            if (ex.Message.Contains("503"))
            {
                log.Error("503 error occured!");
                await ScrapeAll(listOfAllDates, htmlDocument);
            }
            log.Error("This SPECIFIC Error occured: {0}", ex.Message);

            //Thread.Sleep(30000);
            await ScrapeAll(listOfAllDates, htmlDocument);
            throw ex;
        }
    }
}

async Task ScarapeDay(List<string> linksOfTheDay)
{
    var articleLink = string.Empty;
    var articleCommentsLink = string.Empty;

    foreach (var link in linksOfTheDay)
    {
        var idForegin = 0;
        var tempStr = string.Empty;

        // prevent double checking the same links
        if (link == articleLink ||
            link == articleCommentsLink ||
            link.Contains("kratki_novini") ||
            string.IsNullOrEmpty(link))
        {
            // TODO: consider whether we want these articles
            continue;
        }

        if (link.Contains("/comments"))
        {
            if (link.IndexOf("comments") == 0)
            {

            }
            tempStr = link.Substring(0, link.IndexOf("comments"));
            articleLink = linksOfTheDay.Where(x => x == tempStr).FirstOrDefault();
            articleCommentsLink = link;
            idForegin = await RecordDataToDb(htmlDocument, log, context, articleLink, articleCommentsLink, idForegin);
        }
        else
        {
            articleLink = link;
            articleCommentsLink = linksOfTheDay.Where(x => x == link + "comments").FirstOrDefault();
            idForegin = await RecordDataToDb(htmlDocument, log, context, articleLink, articleCommentsLink, idForegin);
        }
    }

    async Task<int> RecordDataToDb(HtmlDocument htmlDocument, Logger log, DnevnikContext context, string? articleLink, string? articleCommentsLink, int idForegin)
    {
        var article = await ScrapeArticle(htmlDocument, articleLink);
        var recordedArticle = await context.AddAsync(article);
        await context.SaveChangesAsync();
        idForegin = recordedArticle.Entity.Id;

        var comments = await ScrapeComments(htmlDocument, articleCommentsLink, idForegin);

        foreach (var comment in comments)
        {
            var currentComments = await context.AddAsync(comment);
        }
        await context.SaveChangesAsync();

        return idForegin;
    }
}

async Task<Article> ScrapeArticle(HtmlDocument htmlDocument, string link)
{
    var article = new Article();
    var sb = new StringBuilder();
    Thread.Sleep(random.Next(2, 25));

    var html = await GetHtmlFromLink(link);
    if (html == null)
    {
        article.Content = "No Text";
        return article;
    }

    htmlDocument.LoadHtml(html);

    //var divContent =
    //htmlDocument
    //.DocumentNode
    //.Descendants("article")
    //.Where(node => node.GetAttributeValue("class", "")
    //.Equals("general-article-v2 article"))
    //.FirstOrDefault();

    var divContent =
htmlDocument
.DocumentNode
.Descendants("div")
.Where(node => node.GetAttributeValue("class", "")
.Equals("site-block"))
.FirstOrDefault();

    if (divContent == null)
    {
        if (link.Contains("/filmi/"))
        {
            article.Title = "Film";
        }

        if (link.Contains("/photos/")) ;
        {
            article.Title = "Photos";
        }
        article.Content = "No Text";
        article.ArticleLink = link;
        return article;
    }

    var title = divContent
   .Descendants("h1")
   .FirstOrDefault()?
   .InnerText.Replace("&quot;", "'").Trim();

    var content = divContent
    .SelectNodes("//div[@class='article-content']");

    if (content == null)
    {
        if (link.Contains("/filmi/"))
        {
            article.Title = "Film";
        }

        if (link.Contains("/photos/"))
        {
            article.Title = "Photos";
        }

        article.Content = "No Text";
        article.ArticleLink = link;
        return article;
    }

    var articleAuthor = divContent
        .Descendants("figcaption")
        .FirstOrDefault()?
        .GetAttributeValue("title", "");

    foreach (var node in content)
    {
        sb.AppendLine(node.InnerText);
    }
    var resultString = Regex.Replace(sb.ToString().Trim(), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline).Replace("&quot;", "'").Replace("&copy; Reuters", "").Replace("&copy; ", "").Trim();

    var datePublished = divContent
        .Descendants("time")
        .FirstOrDefault()?
        .Attributes["datetime"].Value;

    var dateModified = divContent
        .Descendants("meta")
        .Where(node => node.GetAttributeValue("itemprop", "")
        .Equals("dateModified"))
        .FirstOrDefault()?
        .Attributes["content"].Value;

    var views = divContent?
    .Descendants("div").Where(node => node.GetAttributeValue("class", "").Equals("article-tools"))
    .FirstOrDefault()?.InnerText.Split(",", StringSplitOptions.None);

    var keywordsSb = new StringBuilder();
    var keywords = divContent?
        .Descendants("li")
        .Where(node => node.GetAttributeValue("itemprop", "")
        .Equals("keywords"));

    if (keywords != null)
    {
        foreach (var word in keywords)
        {
            keywordsSb.AppendLine(word.InnerText.Trim() + "; ");
        }
    }

    article.Title = title;
    article.Content = resultString;
    article.ArticleLink = link;
    article.Author = string.IsNullOrEmpty(articleAuthor) ? null : articleAuthor;
    article.DateModified = dateModified == null ? null : DateTime.Parse(dateModified).Date;
    article.DatePublished = datePublished == null ? null : DateTime.Parse(datePublished).Date;
    article.Views = string.IsNullOrEmpty(views?[views.Length - 1]) ? null : views[views.Length - 1].Trim().Contains(' ') ? null : views[views.Length - 1].Trim();
    article.Keywords = keywordsSb.ToString();

    return article;

    // Code if we want to record in file
    //var filePath = @"C:\Users\\dkats\Desktop\asdff.csv";
    //var resultString = Regex.Replace(sb.ToString().Trim(), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
    //await File.AppendAllTextAsync(filePath, resultString, Encoding.UTF8);
}



async Task<List<Comment>> ScrapeComments(HtmlDocument htmlDocument, string articleCommentsLink, int foreignKey)
{
    //add null validation 
    var articleComments = new List<Comment>();
    var sb = new StringBuilder();
    Thread.Sleep(random.Next(10, 54));
    var html = await GetHtmlFromLink(articleCommentsLink);

    if (html == null)
    {
        articleComments.Add(new Comment
        {
            ArticleId = foreignKey,
            Content = "No Comments"
        });

        return articleComments;
    }

    htmlDocument.LoadHtml(html);
    var substringOfRating = "Рейтинг: ".Count();

    var divConent =
    htmlDocument
    .DocumentNode
    .Descendants("article")
    .Where(node => node.GetAttributeValue("class", "")
    .Equals("general-article-v2 article"))
    .FirstOrDefault();

    if (divConent == null)
    {
        articleComments.Add(new Comment
        {
            ArticleId = foreignKey,
            Content = "No Comments"
        });
        return articleComments;
    }

    var title = divConent
    .Descendants("h1")
    .FirstOrDefault()?
    .InnerText.Replace("&quot;", "'").Trim();

    var commentsWrapper =
         htmlDocument
        .DocumentNode
        .Descendants("div")
        .Where(node => node.GetAttributeValue("id", "")
        .Equals("comments-wrapper"))
        .FirstOrDefault();

    if (commentsWrapper != null)
    {
        //fix it to take ALL comments
        var allComments = commentsWrapper
                .Descendants("li")
                .Where(x => x.GetAttributeValue("class", "").Contains("comment"));


        if (allComments != null)
        {
            ExtractComments(articleCommentsLink, foreignKey, articleComments, sb, substringOfRating, title, allComments);

            if (allComments.Count() > 99)
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
                var uniqueNumberOfLink = ExtractUniqueNumber(articleCommentsLink);
                //creating a Get request for loading the additional comments
                var commentsOverHundred = await client.GetStringAsync("https://www.dnevnik.bg/ajax/forum/story/" + uniqueNumberOfLink + "/100/1/1/all");
                var commentsInText = JsonConvert.DeserializeObject<DtoJson>(commentsOverHundred);

                if (!string.IsNullOrEmpty(commentsInText?.Result))
                {
                    htmlDocument.LoadHtml(commentsInText.Result);
                    var allOtherComments = htmlDocument
                        .DocumentNode
                        .Descendants("li")
                        .Where(x => x.GetAttributeValue("class", "").Contains("comment"));

                    ExtractComments(articleCommentsLink, foreignKey, articleComments, sb, substringOfRating, title, allOtherComments);
                }
            }
        }

        if (articleComments.Count() == 0)
        {
            articleComments.Add(new Comment
            {
                ArticleId = foreignKey,
                Content = "No Comments"
            });
        }
        return articleComments;
    }
    else
    {
        // no comments
        if (articleComments.Count() == 0)
        {
            articleComments.Add(new Comment
            {
                ArticleId = foreignKey,
                Content = "No Comments"
            });
        }
        return articleComments;
    }

    static void ExtractComments(string articleCommentsLink, int foreignKey, List<Comment> articleComments, StringBuilder sb, int substringOfRating, string? title, IEnumerable<HtmlNode> allComments)
    {
        try
        {
            foreach (var comment in allComments)
            {
                var commentNumber = comment?.Descendants("var").FirstOrDefault()?.InnerText;
                var authorsAccount = comment?.Descendants("h6").FirstOrDefault()?.InnerText;
                var authorsInfo = comment?.Descendants("a").Select(node => node
                    .GetAttributeValue("href", String.Empty)).FirstOrDefault();
                var rating = comment?.Descendants("strong").FirstOrDefault()?.InnerText;
                var authorsRating = 0;

                if (rating == null)
                {
                    authorsRating = 0;
                }
                else
                {
                    if (rating.Substring(substringOfRating) == string.Empty)
                    {
                        authorsRating = 0;

                    }
                    else
                    {
                        authorsRating = int.Parse(rating.Substring(substringOfRating).Trim());
                    }
                }

                var dateOfComment = comment?
                    .Descendants("time")
                    .FirstOrDefault()?
                    .Attributes["datetime"].Value;


                var commentTone = comment?.Descendants("small").FirstOrDefault()?.InnerText;

                var commentContnet = comment?.Descendants("div").Where(node => node.GetAttributeValue("class", "").Equals("cr")).ToList();
                foreach (var node in commentContnet)
                {
                    sb.AppendLine(node.InnerText);
                }
                var allCommentContent = Regex.Replace(sb.ToString().Trim(), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline).Replace("&quot;", "'").Replace("&copy; Reuters", "").Replace("&copy; ", "").Trim();
                sb.Clear();

                // they might be null
                var negativeReactions = comment?.Descendants().Where(node => node.GetAttributeValue("class", "").Equals("e-minus")).FirstOrDefault()?.InnerText;
                var positiveReactions = comment?.Descendants().Where(node => node.GetAttributeValue("class", "").Equals("e-plus")).FirstOrDefault()?.InnerText;

                articleComments.Add(new Comment
                {
                    CommentNumber = commentNumber == null ? 0 : int.Parse(commentNumber),
                    Author = authorsAccount,
                    AuthorsInfo = authorsInfo,
                    AuthorsRating = authorsRating,
                    CommentLink = articleCommentsLink,
                    Tone = commentTone,
                    Content = allCommentContent,
                    DatePosted = dateOfComment == null ? null : DateTime.Parse(dateOfComment).Date,
                    ArticleTitle = title,
                    NegativeReactions = negativeReactions == null ? 0 : int.Parse(negativeReactions),
                    PositiveReactions = positiveReactions == null ? 0 : int.Parse(positiveReactions),
                    ArticleId = foreignKey
                });
            }

        }
        catch (Exception e)
        {

            throw e;
        }
    }
}

string ExtractUniqueNumber(string articleCommentsLink)
{
    int secontToLastIndex = 2;
    var arrFragments = articleCommentsLink.Split('/');
    var buniqueNumber = arrFragments[arrFragments.Length - secontToLastIndex].Split('_');
    return buniqueNumber[0];
}

async Task<string> GetHtmlFromLink(string link)
{
    try
    {

        using (var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }))
        {

            log.Info("currnet link is {0}", link);
            if (link == null)
            {
                log.Error("Passed empty link");
                return null;
            }
            return await httpClient.GetStringAsync(link).ConfigureAwait(false);
        }
    }
    catch (Exception ex)
    {
        //protection against redirection to commercials 
        if (ex.Message.Contains("code does not indicate success: 301"))
        {
            log.Error("Dnevnik Tried to promote commeercials");
            return null;
        }

        if (ex.Message.Contains("code does not indicate success: 302"))
        {
            Console.WriteLine("Dnevnik Tried to redirect to another page! This was the link {0}", link);
            log.Error("Dnevnik Tried to redirect to another page! This was the link {0}", link);
            return null;
        }
        throw ex;
    }

}

async Task<HashSet<string>> TakeAllLinksOfDay(HtmlDocument htmlDocument, string dataInString)
{
    // implement log here
    var url = "https://www.dnevnik.bg/allnews/" + dataInString;
    var html = await GetHtmlFromLink(url);
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

    listLinks.RemoveWhere(x =>
    x.Contains("#event"));

    //||
    //x.Contains("/filmi/") ||
    //x.Contains("/photos/"));

    return listLinks;
}

static IEnumerable<string> EachDay(DateTime from, DateTime thru)
{
    for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
        yield return day.ToString("yyyy/MM/dd");
}

static Func<int, HashSet<string>> FilterLinks(Func<int, HashSet<string>> links = null)
{

    return links;
};





public class DtoJson
{
    public string? Result { get; set; }
}
