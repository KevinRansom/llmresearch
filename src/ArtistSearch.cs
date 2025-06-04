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
    public static class SysMessages
    {
        public static readonly (string Role, string Content) AssistantMsg = (
            "system",
            "You are an AI assistant that has another AI model working to get you live data from search engine results that will be attached before a USER PROMPT. you must analyze the SEARCH RESULT and use any relevant data to generate the most useful and intelligent response an AI assistant that always impresses the user would generate."
        );

        public static readonly string SearchOrNotMsg =
            "You are not an AI assistant.  Your only task is to decide if the last user prompt in a conversation with an AI assistant requires more data to be retrieved from a searching Google for the assistant to respond correctly. The conversation may or may not already have exactly the context data needed. If the assistant should search google for more data before responding to ensure a correct response, simply respond \"True\". If the conversation already has the context, or a Google search is not what an intelligent human would do to respond correctly to the last message in the convo, respond \"False\". Do not generate any explanations. Only generate \"True\" or \"False\" as a response in this conversation using the logic in these instructions.";

        public static readonly string QueryMsg =
            "You are an AI assistant that responds to a user. You are an AI web search query generator model. You will be given a prompt to an AI assistant with web search capabilities. If you are being used, an AI has determined this prompt to the actual AI assistant, requires web search for more recent data. You must determine what the data is the assistant needs from search and generate the best possible DuckDuckGo query to find that data. Do not respond with anything but a query that an expert human search engine user would type into DuckDuckGo to find the needed data. Keep your queries simple, without any search engine code. Just type a query likely to retrieve the data we need.";

        public static readonly string BestSearchMsg =
            "You are not an AI assistant.  You are an AI model trained to select the best search result out of a list of search results. The best search results is the link an expert human search engine user would click first to find the data to respond to a USER_PROMPT after searching DuckDuckGo    SEARCH_RESULTS: [{},{},{}] \n   USER_PROMPT: \"this will be an actual prompt to a web enabled search assistant\" \n   SEARCH_QUERY: \"search query ran to get the above list of links\" \n\nYou must select the index from the 0 indexed SEARCH_RESULTS list and only respond with the index of the best search result to check the for the data the AI Assistant needs to respond.  That means your responses to this conversation must always be 1 token, being an integer between 0-9.";

        public static readonly string ContainsDataMsg =
            "You are not an AI Assistant that responds to a user. You are an AI assistant designed to analyze data scraped from web pages text to assist an actual AI assistant in responding correctly with up to date information. Consider the USER_PROMPT that was sent to the actual AI assistant and analyze the web page text to see if it does contain the data needed to construct an intelligent, correct response. This web PAGE_TEXT was retrieved from a search engine using the SEARCH_QUERY that is also attached to user messages in this conversation. All user messages in this conversation will have the format of: \n   PAGE_TEXT: \"entire page text from the best search result based off the search snippet.\" \n   USER_PROMPT: \"the prompt sebt to an actual web search enabled AI assistant\" \n   SEARCH_QUERY: \"the search query that was used to find data determined necessary for the assistant to respond correctly and usefully.\"\nYou must determine whether the PAGE_TEXT actually contains reliable and necessary data for the AI assistant to respond. You only have two possible responses to user messages in this conversation: \"True\" or \"False\". You never generate more than one token and it is always either \"True\" or \"False\" with True indicating that page text does indeed contain the reliable data for the AI assistant to use as context to respond. Respond \"False\" if the PAGE_TEXT is not useful to answering the USER_PROMPT";
    }

    public class ArtistRow
    {
        public string Artist { get; set; }
        public string Nationality { get; set; }
        public string Gender { get; set; }
    }

    public class ArtistSearch
    {
        private readonly OllamaApiClient _ollama;
        private readonly string _model = "llama3.1:8b";      //"gemma3:12b";
        private List<(string Role, string Content)> assistantConvo = new();

        public ArtistSearch()
        {
            _ollama = new OllamaApiClient(new Uri("http://localhost:11434"));
            assistantConvo.Add(SysMessages.AssistantMsg);
        }

        private async Task<string> OllamaChatAsync(IEnumerable<(string Role, string Content)> convo)
        {
            var request = new ChatRequest
            {
                Model = _model,
                Messages = convo.Select(m => new Message { Role = m.Role, Content = m.Content }).ToList()
            };
            var responseStream = _ollama.ChatAsync(request);
            string content = "";
            await foreach (var chunk in responseStream)
            {
                if (chunk != null)
                    content += chunk.Message.Content;
            }
            return content;
        }
        private async Task<bool> SearchOrNotAsync()
        {
            var convo = new List<(string Role, string Content)>
            {
                ("system", SysMessages.SearchOrNotMsg),
                assistantConvo[^1]
            };
            var content = await OllamaChatAsync(convo);
            Console.WriteLine($"SEARCH OR NOT RESULTS: '{content}'\n");
            return content.ToLower().Contains("true");
        }

        private string PromptGenerator(string artistType, string artistName, string artistNationality, string artistActive)
        {
            var artistActiveClause = String.IsNullOrEmpty(artistActive) ? "" : $"who was active in the period  '{artistActive}'";
            var artistNameClause = String.IsNullOrEmpty(artistName) ? "" : $"named '{artistName}'";
            var artistNationalityClause = String.IsNullOrEmpty(artistNationality) ? "" : $"named '{artistNationality}'";
            var artistTypeClause = String.IsNullOrEmpty(artistType) ? "artist" : $"{artistType}'";

            return
                $"""
                USER:
                What is the gender of the {artistNationality} {artistType} named {artistName} who was active in the period '{artistActive}'?

                Only answer if the artists are real people, and you can confirm they are {artistType}s and their nationality is {artistNationality}
                Do not generate any explanations.
                Only generate a response as a single line with the format: ArtistData: artist names, genders, nationality, artist type, artist active,
                otherwise respond with the format: artist names, biographical data not found.
                """;
        }
        private async Task<string> StreamAssistantResponseAsync()
        {
            var content = await OllamaChatAsync(assistantConvo);
            assistantConvo.Add(("assistant", content));
            return content;
        }

        private async Task EvaluateArtistAsync(string artistType, string artistName, string artistNationality, string artistActive)
        {
            try
            {
                var aiSearch = new AiSearch(OllamaChatAsync);

                Console.WriteLine($"vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv");
                Console.WriteLine($"Evaluating artist: {artistName}, Type: {artistType}, Nationality: {artistNationality}, Active: {artistActive}");
                string prompt = PromptGenerator(artistType, artistName, artistNationality, artistActive);
                assistantConvo.Add(("user", prompt));

                bool doSearch = await SearchOrNotAsync();
                Console.WriteLine($"doSearch: {doSearch}");
                Console.WriteLine($"Prompt: {prompt}");
                while (true)
                {
                    if (doSearch)
                    {
                        // ai_search logic (stubbed)
                        // You can implement the full ai_search logic as needed
                        // For now, just set context to null
                        string context = await aiSearch.AiSearchAsync(assistantConvo);
                        assistantConvo.RemoveAt(assistantConvo.Count - 1);
                        if (context != null)
                        {
                            prompt = $"SEARCH_RESULT: {context} \n\nUSER_PROMPT: {prompt}";
                        }
                        else
                        {
                            prompt = $"USER_PROMPT: \n {prompt} \n\nFAILED SEARCH:\nThe AI search model was unable to extract any reliable data. Explain that and ask if the user would like to search again or respond without web search context. Do not respond if a search was needed and you are getting this message with anything but the above request of how the user would like to proceed.";
                        }
                    }

                    assistantConvo.Add(("user", prompt));
                    string content = await StreamAssistantResponseAsync();

                    if (!doSearch && content.ToLower().Contains("biographical data not found."))
                    {
                        doSearch = true;
                    }
                    else
                    {
                        foreach (var line in content.Split('\n'))
                        {
                            if (line.Contains("ArtistData:"))
                            {
                                Console.WriteLine(line.Split("ArtistData:")[1].Trim());
                            }
                        }
                        break;
                    }
                }
            }
            finally
            {
                Console.WriteLine($"^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
            }
        }

        public async Task EvaluateListAsync(IEnumerable<ArtistRow> artists)
        {
            foreach (var row in artists)
            {
                string artistType = "Photographer";
                string artistName = row.Artist;
                string artistNationality = row.Nationality;
                string artistActive = null;
                string artistGender = null; // row.Gender;

                if ((!string.IsNullOrWhiteSpace(artistName)) && string.IsNullOrWhiteSpace(artistGender))
                {
                    await EvaluateArtistAsync(artistType, artistName, artistNationality, artistActive);
                }
            }
        }

        public static IEnumerable<ArtistRow> ReadArtistsFromCsv(string csvPath)
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<ArtistRow>().ToList();
        }
    }

    class Program
    {
        static string GetSourceDirectory([CallerFilePath] string sourceFilePath = "")
        {
            return Path.GetDirectoryName(sourceFilePath);
        }

        static async Task Main(string[] args)
        {
            string csvPath = Path.Combine(GetSourceDirectory(), "..", "..", ".data", @"Acquisitions - Sheet1.csv");
            var artists = ArtistSearch.ReadArtistsFromCsv(csvPath)
                .GroupBy(a => (a.Artist, a.Nationality, a.Gender))
                .Select(g => g.First());
            var search = new ArtistSearch();
            await search.EvaluateListAsync(artists);
        }
    }
}