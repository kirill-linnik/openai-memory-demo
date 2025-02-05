# Azure OpenAI RAG + memory Demo app

## Infrastructure
Obviously, that uses Azure infrastructure. You will need to create following resources (please, follow the sequence):
1. Azure OpenAI:
	1. Please, create chat completion deployment. `GPT-4o` is used in this case, but feel free to experiment with others.

## Running locally
If you want to test it, you need two things:
1. Install docker
2. Create file, for example `.demo.env` and add the following content there:
```sh
AZURE_OPENAI_CHATGPT_DEPLOYMENT=<name of your chat gpt deployment>
AZURE_OPENAI_ENDPOINT=<endpoint of your Azure Open AI deployment>
AZURE_OPENAI_API_KEY=<key for using your Azure Open AI deployment>
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
Prior running it, you will need to setup these environment variables:

```sh
AZURE_OPENAI_CHATGPT_DEPLOYMENT=<name of your chat gpt deployment model>
AZURE_OPENAI_ENDPOINT=<endpoint of your Azure Open AI deployment>
AZURE_OPENAI_API_KEY=<key for using your Azure Open AI deployment>
```
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