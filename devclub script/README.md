#Script for DevClub travel

## Preparations

Use any GPT service you like. I did it with `Bing Co-pilot`.

### Company demo

Let's create company profile. I used this prompt:

```
I want to create DevClub Tours - company with the focus on serving IT specialists and finding perfect locations for their travels. 

I want you to think about following things: 
- entertaining company introduction
- company vision
- how we select our destinations
- how we support our customers 

You can add any aspects you think important while creating a company like that. 

Highlight some of our destinations like: Argentina, Brazil, France, India, Indonesia, Italy, Peru, Portugal, Thailand, Vietnam

Do it as long text as possible.
```

If you are lazy, you may find generated text in `company intro` folder.

### Countries

prompt 1:

```
Think about top <...pick your number here...> countries for a vacation for an IT guy coming from Estonia. It should have good and cheap internet, good and cheap food and multiple points of interest. I need only the list of countries.
```

For 20 countries, I've got this response:

```
Thailand
Vietnam
Indonesia
Philippines
Malaysia
India
Turkey
Mexico
Brazil
Argentina
Colombia
Peru
Spain
Portugal
Greece
Italy
France
Germany
Czech Republic
Hungary
```

For every country in the list, do the following prompt 2:

```
You are IT person coming from Estonia. Tell me as much as you know about this country: <...country name here...>. 
I want you tell me everything about:
- safety
- IT infrastructure
- food
- public transportation
- points of interests
- culture
- economy
- price of living

Add any other aspects you think important to know about this country too.

Do it as long text as possible.
``` 

Copy to your preferred text editor and store as PDF. Text for some countries (Argentina, Brazil, France, India, Indonesia, Italy, Peru, Portugal, Thailand, Vietnam) are already present in `country texts` folder, but feel free to add more if you like.

### Tours 

This one I did in `PowerPoint`. Click to `Copilot` button, Create presentation and: 

```
Создай презентацию от лица DevClub Tours. Мы предлагаем туры в следующие страны:
- Аргентина
- Бразилия
- Франция
- Индия
- Индонезия
- Италия
- Перу
- Португалия
- Таиланд
- Вьетнам

Для каждой страны создай следующие слайды:
- почему стоит посетить эту страну
- в какие месяца это лучше всего делать
- примерная стоимость тура из Эстонии в евро на 7, 14 и 21 день.
```

As you see, this text is in Russian, an demo can handle it easily. Generated presentation for 10 countries mentioned before is in `tours` folder.

## Run

Run the way you like it (see README in the main folder). Go to documents, and upload these documents. If you are on a free tier, you might want to upload them 1-by-1.
Once done, open two separate browser sessions, say what you like in one, see how it affects your discussion in another chat ;-)