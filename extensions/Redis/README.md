# Kernel Memory with Redis

[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/KMdiscord)

## Notes about Redis Vector Search:

Redis Vector search requires the use of
Redis' [Search and Query capabilities](https://redis.io/docs/interact/search-and-query/).

This is available in:

* [Redis Stack](https://redis.io/docs/about/about-stack/)
* [Azure Cache for Redis](https://azure.microsoft.com/en-us/products/cache) - Enterprise Tier only
* [Redis Cloud](https://app.redislabs.com/)
* [Redis Enterprise](https://redis.io/docs/about/redis-enterprise/)

You can run Redis Stack locally in docker with the following command:

```sh
docker run -p 8001:8001 -p 6379:6379 redis/redis-stack
```

This will run Redis on port 6379, as well as running a popular Redis GUI, [RedisInsight](https://redis.com/redis-enterprise/redis-insight/), on port 8001.

## Configuring Tag Filters

Using tag filters with Redis requires you to to pre-define which tag fields you want. You can
do so using the `RedisMemoryConfiguration.Tags` property (with the characters being the tag separators)
while creating the dependency-injection pipeline. It's important that you pick a separator that will
not appear in your data (otherwise your tags might over-match)

