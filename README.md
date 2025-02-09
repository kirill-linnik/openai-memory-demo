# Azure OpenAI RAG + memory Demo app

## Infrastructure
Obviously, that uses Azure infrastructure. You will need to create following resources (please, follow the sequence):
1. Azure OpenAI. This is our core, which will be replying to all the questions we have.
	1. Create chat completion deployment, that will be used for conversations. `GPT-4o-mini` was used in this case, but feel free to experiment with others.
	2. Create embedding deployment, that will be used for generating embeddings in RAG. `text-embedding-ada-002` was used in this case, but feel free to experiment with others.
2. Azure Storage Account, and we will store RAG files here: 
	1. You also need to create Container there.
3. Azure AI Search, this will be our search enginer for RAG:
	1. No worries, you can start with Free tier.
4. Document Intelligence, this will analyze files and extract text from them:
    1. Free tier is good enough for testing purposes.

## Running locally
If you want to test it, you need two things:
1. Install docker
2. Create file, for example `.demo.env` and add the following content there:
```sh
AZURE_OPENAI_CHATGPT_DEPLOYMENT=<name of your chat completion deployment from 1.1>
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=<name of your embedding deployment from 1.2>
AZURE_OPENAI_ENDPOINT=<endpoint of your Azure Open AI deployment from 1>
AZURE_OPENAI_API_KEY=<key for using your Azure Open AI deployment from 1>
AZURE_STORAGE_CONNECTION_STRING=<conenction string for Azure Storage from 2>
AZURE_STORAGE_CONTAINER_NAME=<container where to store files from 2.1>
AZURE_SEARCH_SERVICE_ENDPOINT=<endpoint of Azure AI Search from 3>
AZURE_SEARCH_SERVICE_API_KEY=<key for Azure AI Search from 3>
AZURE_SEARCH_INDEX_NAME=<name of the index you want to be automatically created>
AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT=<endpoint for Document Intelligence from 4>
AZURE_FORMRECOGNIZER_SERVICE_API_KEY=<key for Document Intelligence from 4>
```

Then you can do:
```sh
docker compose -f docker-compose.yml --env-file .demo.env build
docker compose -f docker-compose.yml --env-file .demo.env up -d
```

Application becomes available at `http://localhost`. Enjoy!

## Developing locally

### Backend
As code is written in `C#`, use `Visual Studio` (`Community Edition` is more than enough). 
Prior running it, you will need to setup the same environment variables as in running locally step.

Then run it as `http`. It should open with `Swagger` on port `5247`.

### Frontend 

As this part is heavy on `TypeScript` (`React`, `Redux`, `Ant Design`), I recommend using `Visual Studio Code`. 
Depending on port used for `Backend` instance, you might want to adjust `BACKEND_HOST_URL` in `./build-configuration/dev.properties` file.
First, you need to do:
```sh
yarn
```
Then start it with:
```sh
yarn start
```
Application becomes available at `http://localhost:8080`.