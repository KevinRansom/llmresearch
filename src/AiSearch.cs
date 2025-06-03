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
        public static async Task<string> ScrapeWebPageAsync(string url)
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
    }
}
