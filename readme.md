# nicklinref_csharp

This is an experimental project that aims to reproduce the functionality of
[`nicklinref_rust`](https://github.com/thehappycheese/nicklinref_rust/tree/main?tab=readme-ov-file)
in C#.

## Usage

For API documentation, please see the
[Usage Section](https://github.com/thehappycheese/nicklinref_rust/tree/main?tab=readme-ov-file#3-usage)
of the readme for the `nicklinref_rust` project, and review the differences section below.

### Features of `nicklinref_rust` to be implemented

This project is in a much earlier state. The following features are still on the todo list

- [ ] The C# version currently uses the route prefix `/line?...` and `/point?...` to disambiguate query types, but the original server chose based on the parameters provided. For compatibility / ease of documentation this restriction can be removed.
- [x] `/batch` endpoint support.
- [x] The `offset=` feature for line and point queries
- [ ] The `cwy=` filter for carriageways is too strict;
  it currently only supports the values `LS`, `RS`, `LRS`. It should also support single items like `L` or badly ordered items like `SL`.
  - [x] If the parameter is omitted, `LRS` is assumed.
- [ ] The web server is not configurable (port used etc)
- [ ] No attempt to handel CORS requirements has been made.
  - It is assumed that something like [caddy](https://caddyserver.com/) would be
    used to add this functionality.
  - Note that proxies such as
    [Azure API Management](https://azure.microsoft.com/en-au/products/api-management)
    do not allow the `null` origin to be explicitly permitted. `Origin:null`
    headers will be met with `Access-Allow-Origin:*` which is no good. It seems
    unavoidable that the proxy MUST support responding with `Access-Allow-Origin:null`.
- [ ] various query validation checks may be missing
  - e.g. Check `slk_from`>=`slk_to` etc
