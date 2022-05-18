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
var cultureInfo = CultureInfo.InvariantCulture;

static IEnumerable<string> EachDay(DateTime from, DateTime thru)
{
    for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
        yield return day.ToString("yyyy/MM/dd");
}

while (listOfAllDates.Any())
{
    var date = listOfAllDates.Pop();
    log.Info("The date about to be scrapted is {0}", date);

    var linksOfADay = new List<string>(await TakeAllLinksOfDay(date));
    if (linksOfADay.Count() == 0)
    {
        Console.WriteLine("OMG");
    }

    if (linksOfADay.Any(x => x == null))
    {
        Console.WriteLine("there is something fishiy here");
    }
    await ScarapeDay(linksOfADay);
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
            tempStr = link.Substring(0, link.IndexOf("/comments"));
            articleLink = listOfAllDates.Where(x => x == tempStr).FirstOrDefault();
            articleCommentsLink = link;

            var article = await ScrapeArticle(httpDocument, articleLink);
            var recordedArticle = await context.AddAsync(article);
            var d = recordedArticle.Entity.Id;
            await context.SaveChangesAsync();
            idForegin = recordedArticle.Entity.Id;

            var comments = await ScrapeComments(httpDocument, articleCommentsLink, idForegin);

            foreach (var comment in comments)
            {
                var currentComments = await context.AddAsync(comment);

                await context.SaveChangesAsync();
            }
        }
        else
        {
            articleLink = link;
            articleCommentsLink = linksOfTheDay.Where(x => x == link + "comments").FirstOrDefault();
            var article = await ScrapeArticle(httpDocument, articleLink);
            var recordedArticle = await context.AddAsync(article);
            var d = recordedArticle.Entity.Id;
            await context.SaveChangesAsync();
            idForegin = recordedArticle.Entity.Id;

            var comments = await ScrapeComments(httpDocument, articleCommentsLink, idForegin);

            foreach (var comment in comments)
            {
                var currentComments = await context.AddAsync(comment);

                await context.SaveChangesAsync();
            }

        }
    }
}

async Task<Article> ScrapeArticle(HtmlDocument htmlDocument, string link)
{
    var article = new Article();
    var sb = new StringBuilder();
    Thread.Sleep(1000);

    var html = await GetHtmlFromLink(link);
    htmlDocument.LoadHtml(html);

    var divContent =
    htmlDocument
    .DocumentNode
    .Descendants("article")
    .Where(node => node.GetAttributeValue("class", "")
    .Equals("general-article-v2 article"))
    .FirstOrDefault();

    if (divContent == null)
    {
        article.Content = "No Text";
        return article;
    }

    var title = divContent
   .Descendants("h1")
   .FirstOrDefault()?
   .InnerText.Replace("&quot;", "'").Trim();

    var content = divContent
    .SelectNodes("//div[@class='article-content']");

    foreach (var node in content)
    {
        sb.AppendLine(node.InnerText);
    }
    var resultString = Regex.Replace(sb.ToString().Trim(), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline).Replace("&quot;", "'").Replace("&copy; Reuters", "").Replace("&copy; ", "").Trim();

    var datePublished = divContent
        .Descendants("time")
        .Where(node => node.GetAttributeValue("itemprop", "")
        .Equals("datePublished"))
        .FirstOrDefault()?
        .Attributes["content"].Value;

    var dateModified = divContent
        .Descendants("meta")
        .Where(node => node.GetAttributeValue("itemprop", "")
        .Equals("dateModified"))
        .FirstOrDefault()?
        .Attributes["content"].Value;

    article.Title = title;
    article.Content = resultString;
    article.ArticleLink = link;
    article.DateModified = DateTime.Parse(dateModified).Date;
    article.DatePublished = DateTime.Parse(datePublished).Date;

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
    Thread.Sleep(1000);

    var html = await GetHtmlFromLink(articleCommentsLink);
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
        articleComments.Add(new Comment { Content = "No Comments" });
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

        var allComments = commentsWrapper
        .SelectNodes("//li[contains(@class,'comment')]");

        if (allComments != null)
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
                    var negativeReactions = comment?.Descendants("span").Where(node => node.GetAttributeValue("class", "").Equals("e-minus")).FirstOrDefault()?.InnerText;
                    var positiveReactions = comment?.Descendants("span").Where(node => node.GetAttributeValue("class", "").Equals("e-plus")).FirstOrDefault()?.InnerText;

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
}

async Task<string> GetHtmlFromLink(string link)
{
    try
    {
        using (var httpClient = new HttpClient())
        {
            log.Info("currnet link is {0}", link);
            if (link == null)
            {
                Console.WriteLine("WTF");
            }
            return await httpClient.GetStringAsync(link).ConfigureAwait(false);
        }
    }
    catch (Exception ex)
    {

        throw ex;
    }

}

static async Task<HashSet<string>> TakeAllLinksOfDay(string dataInString)
{
    // implement log here
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





