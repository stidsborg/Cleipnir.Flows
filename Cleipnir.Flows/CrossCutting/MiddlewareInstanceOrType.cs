using System;

namespace Cleipnir.Flows.CrossCutting;

public record MiddlewareInstanceOrType;

public record MiddlewareInstance(IMiddleware Middleware) : MiddlewareInstanceOrType;
public record MiddlewareType(Type Type) : MiddlewareInstanceOrType;