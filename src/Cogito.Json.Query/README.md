# Cogito.Json.Query

A small query language over JSON, compiled to a .NET delegate.

## Why

Letting users filter data means either accepting expressions you have to trust, or building a query
UI. A narrow query language gives you a third option: an expression users can write, that you can
parse and compile, and that cannot do anything but inspect the document.

## Install

```shell
dotnet add package Cogito.Json.Query
```

## Use

```csharp
var compiler = new JsonQueryCompiler();
var predicate = compiler.Compile("status == 'open' && total > 100");

var matches = documents.Where(predicate);
```

The query compiles to a LINQ expression tree, so evaluation costs no more than the equivalent
hand-written predicate, and the compiled delegate can be cached and reused.

## License

MIT.
