Kernel Memory (^2)
==================

[![License: MIT](https://img.shields.io/github/license/microsoft/kernel-memory)](https://github.com/microsoft/kernel-memory/blob/main/LICENSE)

> [!CAUTION]
> This is an active research project. It is evolving rapidly and may change without notice. Use at your own risk. See [Disclaimer](#disclaimer).

KM² is a full rewrite of the initial research prototype, informed by lessons learned from the first iteration and by adjacent work in this space.

The previous codebase remains in the repo for reference only.

Both versions exist purely as research projects used to explore new ideas and gather feedback from the community.

# What’s next

An important aspect of KM² is how we are building the next memory prototype. In parallel, our team is developing [Amplifier](https://github.com/microsoft/amplifier/tree/next), a platform for metacognitive AI engineering. We use Amplifier to build Amplifier itself — and in the same way, we are using Amplifier to build the next generation of Kernel Memory.

KM² will focus on the following areas, which will be documented in more detail when ready:
- quality of content generated
- privacy
- collaboration

## Disclaimer

> [!IMPORTANT]
> **This is experimental software. _Expect things to break_.**

- Contributions are not accepted at this stage
- No stability or compatibility guarantees
- Pin specific commits if you need consistency
- Intended as a learning resource, not production-ready software
- **No support provided** - see [SUPPORT.md](SUPPORT.md)

Contributors
============

<!--
gh api repos/:owner/:repo/contributors --paginate --jq '
  [ .[] | {
    login: .login,
    id: .id,
    url: .html_url,
    avatar: "https://avatars.githubusercontent.com/u/\(.id)?v=4&s=110"
  } ] as $list
  | (
      "| " + ($list[0:6] | map("<img alt=\"\(.login)\" src=\"\(.avatar)\" width=\"110\">") | join(" | ")) + " |\n" +
      "|:---:|:---:|:---:|:---:|:---:|:---:|\n" +
      "| " + ($list[0:6] | map("[\(.login)](\(.url))") | join(" | ")) + " |\n" +
      (
        reduce range(6; ($list | length); 6) as $i (
          "";
          . + "| " + ($list[$i:$i+6] | map("<img alt=\"\(.login)\" src=\"\(.avatar)\" width=\"110\">") | join(" | ")) + " |\n" +
              "| " + ($list[$i:$i+6] | map("[\(.login)](\(.url))") | join(" | ")) + " |\n"
        )
      )
    )
'
-->

|     <img alt="dluc" src="https://avatars.githubusercontent.com/u/371009?v=4&s=110" width="110">     |  <img alt="marcominerva" src="https://avatars.githubusercontent.com/u/3522534?v=4&s=110" width="110">   |  <img alt="anthonypuppo" src="https://avatars.githubusercontent.com/u/6828951?v=4&s=110" width="110">   |    <img alt="crickman" src="https://avatars.githubusercontent.com/u/66376200?v=4&s=110" width="110">     |  <img alt="TaoChenOSU" src="https://avatars.githubusercontent.com/u/12570346?v=4&s=110" width="110">   |      <img alt="cherchyk" src="https://avatars.githubusercontent.com/u/1703275?v=4&s=110" width="110">       |
|:---------------------------------------------------------------------------------------------------:|:-------------------------------------------------------------------------------------------------------:|:-------------------------------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------------------------------:|:------------------------------------------------------------------------------------------------------:|:-----------------------------------------------------------------------------------------------------------:|
|                                   [dluc](https://github.com/dluc)                                   |                             [marcominerva](https://github.com/marcominerva)                             |                             [anthonypuppo](https://github.com/anthonypuppo)                             |                                 [crickman](https://github.com/crickman)                                  |                              [TaoChenOSU](https://github.com/TaoChenOSU)                               |                                   [cherchyk](https://github.com/cherchyk)                                   |
| <img alt="kbeaugrand" src="https://avatars.githubusercontent.com/u/9513635?v=4&s=110" width="110">  |      <img alt="alexmg" src="https://avatars.githubusercontent.com/u/131293?v=4&s=110" width="110">      |   <img alt="alkampfergit" src="https://avatars.githubusercontent.com/u/358545?v=4&s=110" width="110">   | <img alt="dependabot[bot]" src="https://avatars.githubusercontent.com/u/49699333?v=4&s=110" width="110"> |  <img alt="slorello89" src="https://avatars.githubusercontent.com/u/42971704?v=4&s=110" width="110">   |       <img alt="xbotter" src="https://avatars.githubusercontent.com/u/3634877?v=4&s=110" width="110">       |
|                             [kbeaugrand](https://github.com/kbeaugrand)                             |                                   [alexmg](https://github.com/alexmg)                                   |                             [alkampfergit](https://github.com/alkampfergit)                             |                          [dependabot[bot]](https://github.com/apps/dependabot)                           |                              [slorello89](https://github.com/slorello89)                               |                                    [xbotter](https://github.com/xbotter)                                    |
|  <img alt="westdavidr" src="https://avatars.githubusercontent.com/u/669668?v=4&s=110" width="110">  |    <img alt="luismanez" src="https://avatars.githubusercontent.com/u/9392197?v=4&s=110" width="110">    |  <img alt="afederici75" src="https://avatars.githubusercontent.com/u/13766049?v=4&s=110" width="110">   |      <img alt="koteus" src="https://avatars.githubusercontent.com/u/428201?v=4&s=110" width="110">       |    <img alt="amomra" src="https://avatars.githubusercontent.com/u/11981363?v=4&s=110" width="110">     |      <img alt="lecramr" src="https://avatars.githubusercontent.com/u/20584823?v=4&s=110" width="110">       |
|                             [westdavidr](https://github.com/westdavidr)                             |                                [luismanez](https://github.com/luismanez)                                |                              [afederici75](https://github.com/afederici75)                              |                                   [koteus](https://github.com/koteus)                                    |                                  [amomra](https://github.com/amomra)                                   |                                    [lecramr](https://github.com/lecramr)                                    |
|   <img alt="chaelli" src="https://avatars.githubusercontent.com/u/878151?v=4&s=110" width="110">    |  <img alt="pawarsum12" src="https://avatars.githubusercontent.com/u/136417839?v=4&s=110" width="110">   |   <img alt="aaronpowell" src="https://avatars.githubusercontent.com/u/434140?v=4&s=110" width="110">    |  <img alt="alexibraimov" src="https://avatars.githubusercontent.com/u/59023460?v=4&s=110" width="110">   |   <img alt="akordowski" src="https://avatars.githubusercontent.com/u/9746197?v=4&s=110" width="110">   |     <img alt="coryisakson" src="https://avatars.githubusercontent.com/u/303811?v=4&s=110" width="110">      |
|                                [chaelli](https://github.com/chaelli)                                |                               [pawarsum12](https://github.com/pawarsum12)                               |                              [aaronpowell](https://github.com/aaronpowell)                              |                             [alexibraimov](https://github.com/alexibraimov)                              |                              [akordowski](https://github.com/akordowski)                               |                                [coryisakson](https://github.com/coryisakson)                                |
|   <img alt="DM-98" src="https://avatars.githubusercontent.com/u/10290906?v=4&s=110" width="110">    |   <img alt="EelcoKoster" src="https://avatars.githubusercontent.com/u/3356003?v=4&s=110" width="110">   | <img alt="GraemeJones104" src="https://avatars.githubusercontent.com/u/79144786?v=4&s=110" width="110"> |   <img alt="imranshams" src="https://avatars.githubusercontent.com/u/15226209?v=4&s=110" width="110">    |   <img alt="jurepurgar" src="https://avatars.githubusercontent.com/u/6506920?v=4&s=110" width="110">   |   <img alt="JustinRidings" src="https://avatars.githubusercontent.com/u/49916830?v=4&s=110" width="110">    |
|                                  [DM-98](https://github.com/DM-98)                                  |                              [EelcoKoster](https://github.com/EelcoKoster)                              |                           [GraemeJones104](https://github.com/GraemeJones104)                           |                               [imranshams](https://github.com/imranshams)                                |                              [jurepurgar](https://github.com/jurepurgar)                               |                              [JustinRidings](https://github.com/JustinRidings)                              |
|   <img alt="Foorcee" src="https://avatars.githubusercontent.com/u/5587062?v=4&s=110" width="110">   | <img alt="v-msamovendyuk" src="https://avatars.githubusercontent.com/u/61688766?v=4&s=110" width="110"> |    <img alt="qihangnet" src="https://avatars.githubusercontent.com/u/1784873?v=4&s=110" width="110">    |     <img alt="neel015" src="https://avatars.githubusercontent.com/u/34688460?v=4&s=110" width="110">     |  <img alt="pascalberger" src="https://avatars.githubusercontent.com/u/2190718?v=4&s=110" width="110">  | <img alt="pradeepr-roboticist" src="https://avatars.githubusercontent.com/u/6598307?v=4&s=110" width="110"> |
|                                [Foorcee](https://github.com/Foorcee)                                |                           [v-msamovendyuk](https://github.com/v-msamovendyuk)                           |                                [qihangnet](https://github.com/qihangnet)                                |                                  [neel015](https://github.com/neel015)                                   |                            [pascalberger](https://github.com/pascalberger)                             |                        [pradeepr-roboticist](https://github.com/pradeepr-roboticist)                        |
|    <img alt="setuc" src="https://avatars.githubusercontent.com/u/9305355?v=4&s=110" width="110">    |    <img alt="slapointe" src="https://avatars.githubusercontent.com/u/1054412?v=4&s=110" width="110">    |   <img alt="spenavajr" src="https://avatars.githubusercontent.com/u/96045491?v=4&s=110" width="110">    |     <img alt="tarekgh" src="https://avatars.githubusercontent.com/u/10833894?v=4&s=110" width="110">     | <img alt="teresaqhoang" src="https://avatars.githubusercontent.com/u/125500434?v=4&s=110" width="110"> | <img alt="tomasz-skarzynski" src="https://avatars.githubusercontent.com/u/119002478?v=4&s=110" width="110"> |
|                                  [setuc](https://github.com/setuc)                                  |                                [slapointe](https://github.com/slapointe)                                |                                [spenavajr](https://github.com/spenavajr)                                |                                  [tarekgh](https://github.com/tarekgh)                                   |                            [teresaqhoang](https://github.com/teresaqhoang)                             |                          [tomasz-skarzynski](https://github.com/tomasz-skarzynski)                          |
| <img alt="Valkozaur" src="https://avatars.githubusercontent.com/u/58659526?v=4&s=110" width="110">  |   <img alt="vicperdana" src="https://avatars.githubusercontent.com/u/7114832?v=4&s=110" width="110">    |    <img alt="walexee" src="https://avatars.githubusercontent.com/u/12895846?v=4&s=110" width="110">     |   <img alt="aportillo83" src="https://avatars.githubusercontent.com/u/72951744?v=4&s=110" width="110">   |   <img alt="carlodek" src="https://avatars.githubusercontent.com/u/56030624?v=4&s=110" width="110">    |     <img alt="KSemenenko" src="https://avatars.githubusercontent.com/u/4385716?v=4&s=110" width="110">      |
|                              [Valkozaur](https://github.com/Valkozaur)                              |                               [vicperdana](https://github.com/vicperdana)                               |                                  [walexee](https://github.com/walexee)                                  |                              [aportillo83](https://github.com/aportillo83)                               |                                [carlodek](https://github.com/carlodek)                                 |                                 [KSemenenko](https://github.com/KSemenenko)                                 |
| <img alt="roldengarm" src="https://avatars.githubusercontent.com/u/37638588?v=4&s=110" width="110"> |    <img alt="snakex64" src="https://avatars.githubusercontent.com/u/39806655?v=4&s=110" width="110">    |
|                             [roldengarm](https://github.com/roldengarm)                             |                                 [snakex64](https://github.com/snakex64)                                 |