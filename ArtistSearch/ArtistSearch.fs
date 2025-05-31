open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Linq
open System.Net.Http
open System.Threading.Tasks
open CsvHelper
open HtmlAgilityPack
open OllamaSharp
open OllamaSharp.Models.Chat
open ReverseMarkdown

type SysMessages =
    static member AssistantMsg =
        ("system",
         "You are an AI assistant that has another AI model working to get you live data from search engine results that will be attached before a USER PROMPT. you must analyze the SEARCH RESULT and use any relevant data to generate the most useful and intelligent response an AI assistant that always impresses the user would generate.")

    static member SearchOrNotMsg =
        "You are not an AI assistant.  Your only task is to decide if the last user prompt in a conversation with an AI assistant requires more data to be retrieved from a searching Google for the assistant to respond correctly. The conversation may or may not already have exactly the context data needed. If the assistant should search google for more data before responding to ensure a correct response, simply respond \"True\". If the conversation already has the context, or a Google search is not what an intelligent human would do to respond correctly to the last message in the convo, respond \"False\". Do not generate any explanations. Only generate \"True\" or \"False\" as a response in this conversation using the logic in these instructions."

    static member QueryMsg =
        "You are an AI assistant that responds to a user. You are an AI web search query generator model. You will be given a prompt to an AI assistant with web search capabilities. If you are being used, an AI has determined this prompt to the actual AI assistant, requires web search for more recent data. You must determine what the data is the assistant needs from search and generate the best possible DuckDuckGo query to find that data. Do not respond with anything but a query that an expert human search engine user would type into DuckDuckGo to find the needed data. Keep your queries simple, without any search engine code. Just type a query likely to retrieve the data we need."

    static member BestSearchMsg =
        "You are not an AI assistant.  You are an AI model trained to select the best search result out of a list of search results. The best search results is the link an expert human search engine user would click first to find the data to respond to a USER_PROMPT after searching DuckDuckGo    SEARCH_RESULTS: [{},{},{}] \n   USER_PROMPT: \"this will be an actual prompt to a web enabled search assistant\" \n   SEARCH_QUERY: \"search query ran to get the above list of links\" \n\nYou must select the index from the 0 indexed SEARCH_RESULTS list and only respond with the index of the best search result to check the for the data the AI Assistant needs to respond.  That means your responses to this conversation must always be 1 token, being an integer between 0-9."

    static member ContainsDataMsg =
        "You are not an AI Assistant that responds to a user. You are an AI assistant designed to analyze data scraped from web pages text to assist an actual AI assistant in responding correctly with up to date information. Consider the USER_PROMPT that was sent to the actual AI assistant and analyze the web page text to see if it does contain the data needed to construct an intelligent, correct response. This web PAGE_TEXT was retrieved from a search engine using the SEARCH_QUERY that is also attached to user messages in this conversation. All user messages in this conversation will have the format of: \n   PAGE_TEXT: \"entire page text from the best search result based off the search snippet.\" \n   USER_PROMPT: \"the prompt sebt to an actual web search enabled AI assistant\" \n   SEARCH_QUERY: \"the search query that was used to find data determined necessary for the assistant to respond correctly and usefully.\"\nYou must determine whether the PAGE_TEXT actually contains reliable and necessary data for the AI assistant to respond. You only have two possible responses to user messages in this conversation: \"True\" or \"False\". You never generate more than one token and it is always either \"True\" or \"False\" with True indicating that page text does indeed contain the reliable data for the AI assistant to use as context to respond. Respond \"False\" if the PAGE_TEXT is not useful to answering the USER_PROMPT"

type ArtistRow(Artist: string, Nationality: string, Gender: string) =
    member val Artist = Artist with get, set
    member val Nationality = Nationality with get, set
    member val Gender = Gender with get, set

type ArtistSearch() =
    let ollama = (new OllamaApiClient(Uri("http://localhost:11434")))
    let model = "llama3.1:8b"
    let assistantConvo = ResizeArray<(string * string)>()
    do assistantConvo.Add(SysMessages.AssistantMsg)

    let ollamaChatAsync (convo: seq<string * string>) =
        task {
            let request = ChatRequest()
            request.Model <- model
            request.Messages <- convo |> Seq.map (fun (role, content) -> Message(Role = ChatRole(role), Content = content)) |> Seq.toList
            let responseStream = ollama.ChatAsync(request)
            let mutable content = ""
            let enumerator = responseStream.GetAsyncEnumerator()
            let mutable keepGoing = true
            while keepGoing do
                let! hasNext = enumerator.MoveNextAsync()
                if hasNext then
                    let chunk = enumerator.Current
                    if not (isNull chunk) then
                        content <- content + chunk.Message.Content
                else
                    keepGoing <- false
            return content.Trim()
        }
    let searchOrNotAsync () =
        task {
            let convo =
                [ ("system", SysMessages.SearchOrNotMsg)
                  assistantConvo.[assistantConvo.Count - 1] ]
            let! content = ollamaChatAsync convo
            printfn "SEARCH OR NOT RESULTS: '%s'\n" content
            return content.ToLower().Contains("true")
        }

    let promptGenerator artistType artistName artistNationality artistActive =
        $"USER:\nWhat is the gender of the {artistNationality} {artistType} {artistName} artist who was active in the period {artistActive}? " +
        $"only answer if these artists are real people, and you can confirm they are a {artistType} and their nationality is {artistNationality} " +
        $"Do not generate any explanations. Only generate a response as a single line with the format: ArtistData: artist names, genders, nationality, artist type, artist active\n\n" +
        $"otherwise respond with the format: artist names, biographical data not found.\n\n"

    let queryGeneratorAsync () = task {
        let queryMsg = $"CREATE A SEARCH QUERY FOR THIS PROMPT: \n{snd assistantConvo.[assistantConvo.Count - 1]}"
        let convo =
            [ ("system", SysMessages.QueryMsg)
              ("user", queryMsg) ]
        return! ollamaChatAsync convo
    }

    let duckDuckGoSearchAsync (query: string) = task {
        let results = ResizeArray<Dictionary<string, string>>()
        use client = new HttpClient()
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36")
        let url = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}"
        let! response = client.GetAsync(url)
        response.EnsureSuccessStatusCode() |> ignore
        let! html = response.Content.ReadAsStringAsync()
        let doc = HtmlDocument()
        doc.LoadHtml(html)
        let nodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'result')]")
        if not (isNull nodes) then
            let mutable i = 1
            for result in nodes do
                if i > 10 then () else
                let titleTag = result.SelectSingleNode(".//a[contains(@class, 'result__a')]")
                if not (isNull titleTag) then
                    let link = titleTag.GetAttributeValue("href", "")
                    let snippetTag = result.SelectSingleNode(".//a[contains(@class, 'result__snippet')]")
                    let snippet = if isNull snippetTag then "No description available" else snippetTag.InnerText.Trim()
                    let dict = Dictionary<string, string>()
                    dict.Add("id", string i)
                    dict.Add("link", link)
                    dict.Add("search_description", snippet)
                    results.Add(dict)
                    i <- i + 1
        return List.ofSeq results
    }

    let bestSearchResultAsync (sResults: List<Dictionary<string, string>>) (query: string) = task {
        let bestMsg =
            $"""   SEARCH_RESULTS: {String.Join(", ", sResults)} \n   USER_PROMPT: {assistantConvo.[assistantConvo.Count - 1]} \n   SEARCH_QUERY: {query}"""
        let mutable result = 0
        let mutable attempt = 0
        let mutable done_ = false
        while not done_ && attempt < 2 do
            try
                let convo =
                    [ ("system", SysMessages.BestSearchMsg)
                      ("user", bestMsg) ]
                let! content = ollamaChatAsync convo
                result <- int content
                done_ <- true
            with _ ->
                attempt <- attempt + 1
        return result
    }

    // Stub for trafilatura functionality
    let scrapeWebpage (url: string) =
        use client = new HttpClient()
        let htmlContent = client.GetStringAsync(url).Result
        let config = Config()
        config.UnknownTags <- Config.UnknownTagsOption.PassThrough
        let converter = Converter(config)
        converter.Convert(htmlContent)

    let containsDataNeededAsync (searchContent: string) (query: string) =
        task {
            let neededPrompt =
                $"PAGE_TEXT: {searchContent} \nUSER_PROMPT: {assistantConvo.[assistantConvo.Count - 1]} \nSEARCH_QUERY: {query}"
            let convo =
                [ ("system", SysMessages.ContainsDataMsg)
                  ("user", neededPrompt) ]
            let! content = ollamaChatAsync convo
            return content.ToLower().Contains("true")
        }

    let streamAssistantResponseAsync () = task {
        let! content = ollamaChatAsync assistantConvo
        assistantConvo.Add(("assistant", content))
        return content
    }

    let rec evaluateArtistAsync artistType artistName artistNationality artistActive =
        task {
            let prompt = promptGenerator artistType artistName artistNationality artistActive
            assistantConvo.Add(("user", prompt))
            let! mutableDoSearch = searchOrNotAsync ()
            let mutable doSearch = mutableDoSearch
            let mutable finished = false
            let mutable promptRef = prompt
            while not finished do
                if doSearch then
                    // ai_search logic (stubbed)
                    // You can implement the full ai_search logic as needed
                    // For now, just set context to null
                    let context: string option = None
                    assistantConvo.RemoveAt(assistantConvo.Count - 1)

                    match context with
                    | Some ctx -> promptRef <- $"SEARCH_RESULT: {context} \n\nUSER_PROMPT: {promptRef}"
                    | None -> promptRef <- $"USER_PROMPT: \n {promptRef} \n\nFAILED SEARCH:\nThe AI search model was unable to extract any reliable data. Explain that and ask if the user would like to search again or respond without web search context. Do not respond if a search was needed and you are getting this message with anything but the above request of how the user would like to proceed."
                assistantConvo.Add(("user", promptRef))
                let! content = streamAssistantResponseAsync ()
                if not doSearch && content.ToLower().Contains("biographical data not found.") then
                    doSearch <- true
                else
                    content.Split('\n')
                    |> Array.iter (fun line ->
                        if line.Contains("ArtistData:") then
                            printfn "%s" (line.Split("ArtistData:").[1].Trim()))
                    finished <- true
        }

    member _.EvaluateListAsync(artists: seq<ArtistRow>) = task {
        for row in artists do
            let artistType = "Photographer"
            let artistName = row.Artist
            let artistNationality = row.Nationality
            let artistActive = ""
            let artistGender = row.Gender
            if String.IsNullOrWhiteSpace(artistGender) then
                do! evaluateArtistAsync artistType artistName artistNationality artistActive
    }

    static member ReadArtistsFromCsv(csvPath: string) =
        use reader = new StreamReader(csvPath)
        use csv = new CsvReader(reader, CultureInfo.InvariantCulture)
        csv.GetRecords<ArtistRow>() |> Seq.toList

[<EntryPoint>]
let main argv =
    let csvPath = Path.Combine(__SOURCE_DIRECTORY__, @"..\.data\V&A Photography Acquisitions 1964-2022 Elizabeth Ransom and Corinne Whitehouse Edit - Sheet1.csv")
    printfn $"{csvPath}"
    let artists =
        ArtistSearch.ReadArtistsFromCsv(csvPath)
        |> Seq.groupBy (fun a -> (a.Artist, a.Nationality, a.Gender))
        |> Seq.map (fun (_, g) -> Seq.head g)
    let search = ArtistSearch()
    search.EvaluateListAsync(artists).GetAwaiter().GetResult()
    0