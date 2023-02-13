<p align="center">
    <img src="https://raw.githubusercontent.com/Draco-lang/Language-suggestions/main/Resources/Logo-Long.svg#gh-light-mode-only" width=60%>
    <img src="https://raw.githubusercontent.com/Draco-lang/Language-suggestions/main/Resources/Logo-Long-Inverted.svg#gh-dark-mode-only" width=60%>
</p>

<h1 align="center">The Draco programming language compiler</h1>

<p align="center">
    <a href="http://discord.draco-lang.org/"><img alt="Discord" src="https://badgen.net/discord/members/gHfhpPFzYu?icon=discord&color=D70&label=Join+our+Discord!"></a>
    <a href="https://playground.draco-lang.org/"><img alt="Online Playground" src="https://img.shields.io/badge/Online-Playground-009a95.svg?logo=data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4NCjxzdmcgd2lkdGg9IjgwIiBoZWlnaHQ9Ijc2IiB2aWV3Qm94PSIwIDAgNzkgNzYiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+DQo8cGF0aCBkPSJtNTEgMjZzLTMtMS0yLTVsNy02LTExLTlzLTktNS0xNC02YzAgMCAzIDQgMSA1bC02IDFzMjMgMTUgMTggMjRjMCAwIDUtMSA3LTR6IiBmaWxsPSIjMDA5YTk0Ii8+DQo8cGF0aCBkPSJtNDcgMThzLTQtOC0xNy0xMWMwIDAtNy0zLTIwLTJsLTEwIDIgMTAgMXMzIDIgNSA2bC0yIDUtMyAzczctMiAxMSAwYzAgMCAyIDAgMyAyIDAgMCAyIDIgMSA0bC0yIDQgNi0zczQtMSA2IDFjMCAwIDYgMiA3IDVsNS0yLTItOHMwLTQgMi03eiIgZmlsbD0iIzAwYzhiZCIvPg0KPHBhdGggZD0ibTQzIDMxdjQ0aDE3czE0LTEgMTgtMTVjMCAwIDMtOS0yLTE4IDAgMC00LTktMTUtMTB6bTExIDljOSAwIDEyIDUgMTIgNSAzIDMgMyAxMCAzIDEwLTEgNS0zIDctMyA3LTQgNi0xMiA0LTEyIDR2LTI2em0tMTEgMzVoLTRzNC00IDQtOXoiIGZpbGw9IiNmZmYiLz4NCjxwYXRoIHRyYW5zZm9ybT0ibWF0cml4KC42NTg2IDAgMCAuNjY2NiAtNSAtODMpIiBkPSJtMjIgMjEzczEgMTEgOSAxOGMwIDAgOCA2IDE2IDcgMCAwIDkgMSAxOS0ybDEwLTQgOS04IDMtNiAyLTYtMi0yczAgNS00IDEwYzAgMC01IDYtMTIgOSAwIDAtNiA0LTE1IDUgMCAwLTggMS0xNi00IDAgMC00LTEtNy04IDAgMC0yLTQtMS03bDMtOXptMTQtN3M1LTggMTMtMTJjMCAwIDUtMyAxMS0zIDAgMCA3LTEgMTIgMSAwIDAgMjItOSAyNy0yMGwzLTExdi0xMnMxLTQgNC02YzAgMCAxLTIgNC0xbDEwIDQgNiAyIDEtMy01LTUtNC02LTEtMnMtMTAtOC0yNCAwYzAgMCAxMi01IDE3IDEgMCAwLTYgMC0xMiA1IDAgMC0zIDItNCA1IDAgMC0yIDItMyA4djRsLTQgNXMtMyA0LTEwIDdsLTIwIDVzLTEwIDItMTggOWMwIDAtOSA2LTEzIDE2IDAgMC00IDUtNCAxNnoiIGZpbGw9IiMwMGM4YmQiIHN0cm9rZT0iIzAwYzhiZCIgc3Ryb2tlLXdpZHRoPSIuMiIvPg0KPHBhdGggZD0ibTUzIDQxczEwLTcgOC05bC0xOC0xdjE0czUgMCAxMC00IiBmaWxsPSIjZmZmIi8+DQo8cGF0aCBkPSJtNDMgNThzNyAwIDExLTRjMCAwIDMtMiA0LTYgMCAwIDEtMi0yLTMgMCAwIDIgMSAxIDMgMCAwLTEgMy02IDVsLTggMi0xIDIgMSAxeiIgZmlsbD0iIzAwYzhiZCIvPg0KPHBhdGggZD0ibTQzIDMxaC00czMgNCA0IDljMCAwIDMtOCAwLTl6IiBmaWxsPSIjZmZmIi8+DQo8L3N2Zz4NCg=="></a>
    <a href="https://www.nuget.org/packages?q=Draco+owner%3ADraco-lang"><img src="https://img.shields.io/nuget/v/Draco.Compiler.svg?logo=nuget" title="Go To Nuget Package" /></a>
</p>

### What is this?
This is the repository for the work-in-progress compiler for the Draco programming language, a new .NET programming language under development. If you want further details on the language itself or want to contribute ideas, head over to the [language suggestions repository](https://github.com/Draco-lang/Language-suggestions).

### Try it out
You can either use the online [playground](https://playground.draco-lang.org/), or you can play with it locally, for that look at the [Getting started guide](Getting-Started.md)

### Roadmap

 * Syntax analysis
   * [x] Lexing
   * [x] Parsing
   * [x] Red-green trees
 * Semantic analysis
   * [x] Symbol resolution
   * [x] Type checking
   * [x] Type inference
   * [x] Dataflow analysis
 * Codegen
   * [x] AST
   * [x] Lowering
   * [x] Custom IR
   * [x] Writing IL
   * [x] Writing PE
 * Optimization
   * [x] TCO
   * [ ] Vectorization
