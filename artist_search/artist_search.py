# Original by  https://www.youtube.com/@Ai_Austin
import ollama
import sys_msgs
import requests
from bs4 import BeautifulSoup
import trafilatura
import pandas as pd


assistant_model = 'llama3.1:8b'        #'llama3.1:8b' 'qwen3:8b' 'qwen3:14b'
assistant_convo = [sys_msgs.assistant_msg]

def search_or_not():
    sys_msg = sys_msgs.search_or_not_msg

    response = ollama.chat(
        model = assistant_model,
        messages = [{'role': 'system', 'content': sys_msg}, assistant_convo[-1]]
    )

    #print(f"SEARCH OR NOT RESULTS: {content}")
    content = response['message']['content']

    if 'true' in content.lower():
        return True
    else:
        return False

def prompt_generator(artist_type, artist_name, artist_nationality, artist_active):
    return (
        f'USER:\nWhat is the gender of the {artist_nationality} {artist_type} {artist_name} artist who was active in the period {artist_active}? '
        f'only answer if these artists are real people, and you can confirm they are a {artist_type} and their nationality is {artist_nationality} '
        f'Do not generate any explanations. Only generate a response as a single line with the format: ArtistData: artist names, genders, nationality, artist type, artist active\n\n'
        f'otherwise respond with the format: artist names, biographical data not found.\n\n'
    )

def query_generator():

    sys_msg = sys_msgs.query_msg
    query_msg = f'CREATE A SEARCH QUERY FOR THIS PROMPT: \n{assistant_convo[-1]}'

    response = ollama.chat(
        model = assistant_model,
        messages = [{'role': 'system', 'content': sys_msg}, {'role': 'user', 'content': query_msg}]
    )

    return response['message']['content']

def duckduckgo_search(query):
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36"
    }
    url = f'https://html.duckduckgo.com/html/?q={query}'
    response = requests.get(url, headers=headers)
    response.raise_for_status()

    soup = BeautifulSoup(response.text, 'html.parser')
    results = []

    for i, result in enumerate(soup.find_all('div', class_='result'), start=1):
        if i > 10:
            break

        title_tag = result.find('a', class_='result__a')
        if not title_tag:
            continue

        link = title_tag['href']
        snippet_tag = result.find('a', class_='result__snippet')
        snippet = snippet_tag.text.strip() if snippet_tag else 'No description available'

        results.append({
            'id': i,
            'link': link,
            'search_description': snippet
        })

    return results

def best_search_result(s_results, query):
    sys_msg = sys_msgs.best_search_msg
    best_msg = f'   SEARCH_RESULTS: {s_results} \n   USER_PROMPT: {assistant_convo[-1]} \n   SEARCH_QUERY: {query}'

    for _ in range(2):
        try:
            response = ollama.chat(
                model = assistant_model,
                messages = [{'role': 'system', 'content': sys_msg}, {'role': 'user', 'content': best_msg}]
            )
            return int( response['message']['content'])
        except:
            continue            

    return 0

def scrape_webpage(url):
    try:
        downloaded = trafilatura.fetch_url(url=url)
        return trafilatura.extract(downloaded, include_formatting=True, include_links=True)
    except Exception as e:
        print(f"Error scraping webpage: {e}")
        return None

def ai_search():
    context = None
    search_query = query_generator()
    if search_query[0] == '"':
        search_query=search_query[1:-1]

    search_results = duckduckgo_search(search_query)
    context_found = False

    while not context_found and len(search_results) > 0:
        best_result = best_search_result(s_results=search_results, query=search_query)
        #print(f'BEST SEARCH RESULT INDEX: {best_result}')
        try:
            page_link = search_results[best_result]['link']
        except:
            print('FAILED TO SELECT BEST SEARCH RESULT, TRYING AGAIN.')
            break

        page_text = scrape_webpage(page_link)
        search_results.pop(best_result)

        if page_text and contains_data_needed(search_content=page_text, query=search_query):
            context = page_text
            context_found = True
            #print('CONTEXT FOUND.')

    return context

def contains_data_needed(search_content, query):
    sys_msg = sys_msgs.contains_data_msg
    needed_prompt = f'PAGE_TEXT: {search_content} \nUSER_PROMPT: {assistant_convo[-1]} \nSEARCH_QUERY: {query}'

    response = ollama.chat(
        model = assistant_model,
        messages = [{'role': 'system', 'content': sys_msg}, {'role': 'user', 'content': needed_prompt}]
    )

    content = response['message']['content']

    if 'true' in content.lower():
        return True
    else:
        return False

def stream_assistant_response():
    global assistant_convo
    response_stream = ollama.chat(model='llama3.1:8b', messages=assistant_convo, stream=True)
    complete_response = ''

    for chunk in response_stream:
        # print (chunk['message']['content'], end='', flush=True)
        complete_response += chunk['message']['content']
    
    assistant_convo.append({'role': 'assistant', 'content': complete_response})
    #print('\n\n')
    return complete_response

def evaluate_artist(artist_type, artist_name, artist_nationality, artist_active):
    global assistant_convo

    prompt = prompt_generator(
        artist_type=artist_type,
        artist_name=artist_name,
        artist_nationality=artist_nationality,
        artist_active=artist_active)

    assistant_convo.append({'role': 'user', 'content': prompt})

    do_search = search_or_not()
    while True:
        if do_search :
            context = ai_search()
            assistant_convo = assistant_convo[:-1]
            if context:
                prompt = f'SEARCH_RESULT: {context} \n\nUSER_PROMPT: {prompt}'
            else:
                prompt = (
                    f'USER_PROMPT: \n {prompt} \n\nFAILED SEARCH:\nThe '
                    'AI search model was unable to extractany reliable data. Explain that '
                    'and ask if the user would like to search again or respond '
                    'without web search context. Do not respond if a search was needed '
                    'and you are getting this message with anything but the above request '
                    'of how the user would like to proceed.'
                )

        assistant_convo.append({'role': 'user', 'content': prompt})
        content = stream_assistant_response()

        if not(do_search) and 'biographical data not found.' in content.lower():
            # Asked ai but no data was found try again
            do_search = True
        else:
            for line in content.splitlines():
                if "ArtistData:" in line:
                    print(line.partition("ArtistData:")[2].strip())
            break

def evaluate_list(artists):

    idx = 0
    for _, row in artists.iterrows():
        artist_type = "Photographer"                            #input('Artist type (Photographer, Painter etc ..): \n')
        #artist_name = row["Name"]                               #input('Name: \n')
        artist_name = row["Artist"]                               #input('Artist: \n')
        artist_nationality = row["Nationality"]                 #input('Nationality\n')
        artist_active = ""  #     row["Decade"]                           #input('Century active\n')
        artist_gender = row["Gender"]                           #input('Artist gender: \n')

        #use ai to turn this into names, nationality, active etc.
        #brief_description = row["Brief Description"]            #input('Brief Description: \n')

        if pd.isna(artist_gender):
            evaluate_artist(
                artist_type = artist_type,
                artist_name = artist_name,
                artist_nationality = artist_nationality,
                artist_active = artist_active)

        idx = idx + 1

def main():
    csv_path = r"C:\Users\codec\Downloads\V&A Photography Acquisitions 1964-2022 Elizabeth Ransom and Corinne Whitehouse Edit - Sheet1.csv"

    df = pd.read_csv(csv_path)
#    artists = df[["Name", "Nationality", "Decade"]].drop_duplicates()
    artists = df[["Artist", "Nationality", "Gender"]].drop_duplicates()
    evaluate_list(artists)

if __name__ == '__main__':
    main()
