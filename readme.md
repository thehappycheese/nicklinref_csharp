# nicklinref_csharp <!-- omit in toc -->

This is an experimental project that aims to reproduce the functionality of
[`nicklinref_rust`](https://github.com/thehappycheese/nicklinref_rust/tree/main?tab=readme-ov-file)
in C#.

- [1. NickMapBI](#1-nickmapbi)
- [2. Performance](#2-performance)
- [3. Usage](#3-usage)
- [4. Features of `nicklinref_rust` to be Implemented](#4-features-of-nicklinref_rust-to-be-implemented)
    - [4.0.1. Stretch Goals](#401-stretch-goals)


## 1. NickMapBI

This project is feature complete enough to connect with [NickMap-BI](https://github.com/thehappycheese/nickmap-bi). Change the Hard Coded URLs on

- line 131 of `src/linref.ts`
- line 445 of `src/nickmap/NickMap.tsx`

## 2. Performance

| Test                               | Rust                                                                           | C#                              |
| ---------------------------------- | ------------------------------------------------------------------------------ | ------------------------------- |
| 6500 features one by one           | 14 seconds                                                                     | 52 seconds                      |
| 6500 features in batches of 1500   | 250 milliseconds per batch                                                     | 900 milliseconds per batch      |
| From PowerBI Visual 30000 features | 2.5 seconds                                                                    | 4.3 seconds                     |
| RAM Usage                          | 70 MB                                                                          | 700 MB                          |
| Tested on                          | Runs with acceptable performance on any potato machine including Raspberry Pi 3 | ?? works ok on github codespace |

## 3. Usage

For API documentation, please see the
[Usage Section](https://github.com/thehappycheese/nicklinref_rust/tree/main?tab=readme-ov-file#3-usage)
of the readme for the `nicklinref_rust` project, and review the differences section below.

## 4. Features of `nicklinref_rust` to be Implemented

This project is in a much earlier state. The following features are still on the todo list

- [x] The C# version currently uses the route prefix `/line?...` and
  `/point?...` to disambiguate query types, but the original server chose based
  on the parameters provided. For compatibility / ease of documentation this
  restriction can be removed.
- [x] `/show/` feature which is useful for testing queries when
  using the endpoint, normally when using the excel =WEBSERVICE() formula, or in
  some python batch process.
- [x] `/batch` endpoint support.
- [x] The `offset=` feature for line and point queries
- [x] Handel CORS requirements
  - Something like [caddy](https://caddyserver.com/) can be used to add this
    functionality instead of coding it into C# like i have done here.
  - Note that proxies such as
    [Azure API Management](https://azure.microsoft.com/en-au/products/api-management)
    do not allow the `null` origin to be explicitly permitted. `Origin:null`
    headers will be met with `Access-Control-Allow-Origin:*` which is no good. It seems
    unavoidable that the proxy MUST support responding with
    `Access-Control-Allow-Origin:null`.
  - Even the built in C# CORS middleware did not permit the null behavior. I had
    to roll my own `PermissiveCORSService`
- [x] Echo Header `x-request-id`
- [x] Handle just the `f=latlon` datatype for points results. This is needed for
  drop-in compatibility so the 'goto' feature works in nickmap-bi
- [x] Handle missing `f=` or `f=json`

#### 4.0.1. Stretch Goals

- [ ] Handle all the other values of `f=` parameter to select response type; eg wkt, geojson etc.
- [ ] various query validation checks may be missing
  - e.g. Check `slk_from`>=`slk_to` etc
- [ ] TThe `cwy=` filter for carriageways is too strict. It currently only
  supports the values `LS`, `RS`, `LRS`. It should also support single items like `L`
  or badly ordered items like `SL`.
  - [x] If the parameter is omitted, `LRS` is assumed.
- [ ] The web server is not configurable (port used etc)
