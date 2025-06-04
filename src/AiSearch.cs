using CsvHelper;
using HtmlAgilityPack;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using ReverseMarkdown;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ArtistSearch
{
    internal class AiSearch
    {
        private readonly Func<IEnumerable<(string Role, string Content)>, Task<string>> ollamaChatAsync;

        public AiSearch(Func<IEnumerable<(string Role, string Content)>, Task<string>> OllamaChatAsync)
        {
            ollamaChatAsync = OllamaChatAsync;
        }

        public Task<string> QueryGeneratorAsync(List<(string Role, string Content)> assistantConvo)
        {
            string queryMsg = $"CREATE A SEARCH QUERY FOR THIS PROMPT: \n{assistantConvo[^1].Content}";
            var convo = new List<(string Role, string Content)>
            {
                ("system", SysMessages.QueryMsg),
                ("user", queryMsg)
            };

            return ollamaChatAsync(convo);
        }

        public async Task<string> ScrapeWebPageAsync(string url)
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
                string htmlContent = await client.GetStringAsync(url);

                var config = new Config { UnknownTags = Config.UnknownTagsOption.PassThrough };
                var converter = new Converter(config);
                return converter.Convert(htmlContent);
            }
            catch(Exception e)
            {
                Console.WriteLine($"{e}");
                return "";
            }
        }
        public async Task<string> AiSearchAsync(List<(string Role, string Content)> assistantConvo)
        {
            string context = null;
            var searchQuery = (await QueryGeneratorAsync(assistantConvo)).Trim('"');
            var searchResults = await DuckDuckGoSearchAsync(searchQuery);
            var contextFound = false;
            while (!contextFound && searchResults.Count > 0)
            {
                int bestResult = await BestSearchResultAsync(searchResults, searchQuery, assistantConvo);

                string pageLink;
                try
                {
                    pageLink = searchResults[bestResult]["link"];
                }
                catch
                {
                    Console.WriteLine("FAILED TO SELECT BEST SEARCH RESULT, TRYING AGAIN.");
                    break;
                }

                string pageText = await ScrapeWebPageAsync(pageLink);
                searchResults.RemoveAt(bestResult);

                if (!string.IsNullOrEmpty(pageText) && await ContainsDataNeededAsync(pageText, searchQuery, assistantConvo))
                {
                    context = pageText;
                    contextFound = true;
                }
            }
            return context;
        }

        private async Task<bool> ContainsDataNeededAsync(string searchContent, string query, List<(string Role, string Content)> assistantConvo)
        {
            string neededPrompt = $"PAGE_TEXT: {searchContent} \nUSER_PROMPT: {assistantConvo[^1]} \nSEARCH_QUERY: {query}";
            var convo = new List<(string Role, string Content)>
            {
                ("system", SysMessages.ContainsDataMsg),
                ("user", neededPrompt)
            };
            var content = await ollamaChatAsync(convo);
            return content.ToLower().Contains("true");
        }

        private async Task<List<Dictionary<string, string>>> DuckDuckGoSearchAsync(string query)
        {
            var results = new List<Dictionary<string, string>>();
            using (var client = new HttpClient())
            {
                var links = new HashSet<string>();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
                string url = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                int i = 1;
                var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'result')]");
                if (nodes != null)
                {
                    foreach (var result in nodes)
                    {
                        var titleTag = result.SelectSingleNode(".//a[contains(@class, 'result__a')]");
                        if (titleTag == null) continue;

                        var link = titleTag.GetAttributeValue("href", "");
                        var snippetTag = result.SelectSingleNode(".//a[contains(@class, 'result__snippet')]");
                        var snippet = snippetTag != null ? snippetTag.InnerText.Trim() : "No description available";
                        if (!String.IsNullOrEmpty(link) && !links.Contains(link))
                        {
                            links.Add(link);
                            Console.WriteLine($"id: {i.ToString()}, link: {link}, search_description: {snippet}");
                            results.Add(new Dictionary<string, string>
                            {
                                { "id", i.ToString() },
                                { "link", link },
                                { "search_description", snippet }
                            });
                            i++;
                        }
                    }
                }
            }
            return results;
        }

        private async Task<int> BestSearchResultAsync(List<Dictionary<string, string>> sResults, string query, List<(string Role, string Content)> assistantConvo)
        {
            string bestMsg = $"   SEARCH_RESULTS: [{string.Join(", ", sResults.Select(r => $"{{'id': {r["id"]}, 'link': '{r["link"]}', 'search_description': '{r["search_description"].Replace("'", "\\'")}'}}"))}] \n   USER_PROMPT: {assistantConvo[^1]} \n   SEARCH_QUERY: {query}";
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    var convo = new List<(string Role, string Content)>
                    {
                        ("system", SysMessages.BestSearchMsg),
                        ("user", bestMsg)
                    };
                    var content = await ollamaChatAsync(convo);
                    return int.Parse(content);
                }
                catch
                {
                    continue;
                }
            }
            return 0;
        }
    }
}
