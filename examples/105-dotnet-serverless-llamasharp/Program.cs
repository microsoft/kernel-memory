// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Llama;
using Microsoft.KernelMemory.AI.Tokenizers;

var llamaConfig = new LlamaSharpConfig();
var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();

new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build()
    .BindSection("KernelMemory:Services:LlamaSharp", llamaConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);

var searchClientConfig = new SearchClientConfig
{
    MaxMatchesCount = 2,
    AnswerTokens = 100,
};

var memory = new KernelMemoryBuilder()
    .WithSearchClientConfig(searchClientConfig)
    .WithLlamaTextGeneration(llamaConfig)
    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig, new DefaultGPTTokenizer())
    .Build<MemoryServerless>();

// Some sci-fi content based on a recent news from ISS
var story = """
            A strange and surprising event transpired upon the celestial manmade globe - the International Space Station. A vegetable of the red fruit variety, otherwise known on our terrestrial land as a 'tomato', was cultivated with the remarkable method of hydroponics, defying the hitherto believed necessity of soil for growth, and subsequently misplaced by the American Voyager, Mr. Frank Rubio.
            As trivial as it may seem, the plantation of this tomato held great significance, being the inaugural produce of its kind to flourish in the challenging conditions of the cosmos, and its inexplicable disappearance made for a comical investigation of sorts. Mr. Rubio, convinced of its safekeeping, found the prize fruit astray and upon his return to the Earth, the bewitching mystery of the vanishing tomato persisted.
            Much to the disquiet of Rubio, accusations of him having consumed the invaluable specimen disquieted the floating abode. He vehemently refuted the charges, attributing the disappearance to the curious character of the conditions in space, where objects not securely affixed could easily drift into unforeseen corners of the spacious station. Despite his rigorous search, the tomato evaded discovery.
            This incident of mirth, notwithstanding, Mr. Rubio's sojourn in space did not stay deprived of notable triumph. His stay in this amidst the heavenly spheres reached a duration hitherto unknown to any American voyager, marking a full Earth-year in space. Rendered longer owing to an unfortunate leak detected in his Russian Soyuz spacecraft, it proved to be a challenging, yet rewarding journey for Rubio.
            A resolution to the tale of the missing tomato finally came not during Mr. Rubio's stay, but with the revelation of the crew remaining in the station of the discovery of the missing specimen. Thus, even after returning to the terrestrial sphere, the voyager's innocence was ultimately affirmed, adding a closing chapter to this historical space oddity.
            Alas, despite the humour this event bequeathed, the great strides made in the science of celestial agriculture cannot be understated. The successful cultivation of a tomato under such harsh conditions bodes well for future endeavours of similar nature, serving as a promising beacon of mankind's progress against the unique challenges that space exploration poses.
            Id est, Rubio's 'lost in space' tomato sparks a shift from jest to marvel, creating a newfound appreciation for the advancements in scientific know-how, that led to the cultivation, and eventual rediscovery of a humble fruit in space.
            Mindful of the peculiar incident, the space administration contrived to install advanced object-tracking systems within the Space Station to avoid recurrent miscellany loss. A new regimen was also introduced to ensure that harvested produce was promptly accounted for and preserved, preventing any further produce-related mysteries.
            Simultaneously, this whimsical incident spurred a new stream of scientific study centered around the longevity and preservation of biotic material in a microgravity environment. Scientists discovered that the space-cultivated tomato, despite its desiccated state, presented unique characteristics not found in its Earth-grown counterparts.
            Detailed analysis revealed heightened concentrations of lycopene in the space-grown tomato, a potent antioxidant known for its numerous health benefits including reducing the risk of heart diseases and cancer. It was debated whether these enhanced features were a byproduct of the tomato's prolonged exposure to cosmic radiation or the unique hydroponic growth methodology adopted on the space station.
            Additionally, the longevity of the tomato in an un-refrigerated state sparked interest in bio-engineering crops for greater longevity on Earth, with potential implications for reducing food waste. The space life of the tomato, in all its humour and seriousness, may mark the beginning of far-reaching advancements in botanical sciences and space exploration.
            In a surprising twist to the tale, around the time the elusive tomato was found, the crew on the space station also stumbled upon something extraordinary — an unidentified substance found growing alongside the microgravity tomatoes. Initially thought to be a mold or fungus, subsequent analysis revealed an organic composition unlike anything known to Earth-bound biology.
            Appearing as a glowing, translucent mold, this substance showed a remarkable rate of growth and exhibited photosynthetic properties, drawing energy not just from sunlight, but also from other forms of radiation. It was able to adapt quickly to the environmental conditions of the space station, including its high CO2 levels.
            Gerald Marshall, the Chief Scientist on the team at NASA, said during a press briefing, "Our initial findings lead us to believe the matter is not terrestrial. Its unprecedented radiant energy conversion efficiency and adaptability are akin to, but far exceed, those seen in extremophile organisms on Earth. We are eager to undertake a comprehensive study and certainly, this could potentially mark a new chapter in astrobiological research."
            While further studies are underway, this intriguing finding sparked a flurry of interest and speculation within and outside the scientific community. This new organic matter, playfully named ‘Rubio's Radiant Mold’ in honor of astronaut Frank Rubio, could potentially reshape our understanding of life in the cosmos and further blur the lines between science fiction and reality. With each passing day, the 'final frontier' appears to become more familiar and intriguingly alien at the same time.
            """;

await memory.ImportTextAsync(story, documentId: "tomato01");

var question = "What happened to the tomato disappeared on the International Space Station?";
Console.WriteLine($"Question: {question}");

var answer = await memory.AskAsync(question);
Console.WriteLine($"Answer: {answer.Result}");

await memory.DeleteDocumentAsync("tomato01");
