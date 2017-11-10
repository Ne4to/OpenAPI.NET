﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers.ParseNodes;

namespace Microsoft.OpenApi.Readers.V2
{
    /// <summary>
    /// Class containing logic to deserialize Open API V2 document into
    /// runtime Open API object model.
    /// </summary>
    internal static partial class OpenApiV2Deserializer
    {
        public static FixedFieldMap<OpenApiDocument> OpenApiFixedFields = new FixedFieldMap<OpenApiDocument>
        {
            {
                "swagger", (o, n) =>
                {
                    /* Ignore it */
                }
            },
            {"info", (o, n) => o.Info = LoadInfo(n)},
            {"host", (o, n) => n.Context.SetTempStorage("host", n.GetScalarValue())},
            {"basePath", (o, n) => n.Context.SetTempStorage("basePath", n.GetScalarValue())},
            {
                "schemes", (o, n) => n.Context.SetTempStorage(
                    "schemes",
                    n.CreateSimpleList(
                        s =>
                        {
                            return s.GetScalarValue();
                        }))
            },
            {
                "consumes",
                (o, n) => n.Context.SetTempStorage("globalconsumes", n.CreateSimpleList(s => s.GetScalarValue()))
            },
            {
                "produces",
                (o, n) => n.Context.SetTempStorage("globalproduces", n.CreateSimpleList(s => s.GetScalarValue()))
            },
            {"paths", (o, n) => o.Paths = LoadPaths(n)},
            {"definitions", (o, n) => o.Components.Schemas = n.CreateMapWithReference("#/definitions/", LoadSchema)},
            {
                "parameters",
                (o, n) => o.Components.Parameters = n.CreateMapWithReference("#/parameters/", LoadParameter)
            },
            {"responses", (o, n) => o.Components.Responses = n.CreateMap(LoadResponse)},
            {"securityDefinitions", (o, n) => o.Components.SecuritySchemes = n.CreateMap(LoadSecurityScheme)},
            {"security", (o, n) => o.SecurityRequirements = n.CreateList(LoadSecurityRequirement)},
            {"tags", (o, n) => o.Tags = n.CreateList(LoadTag)},
            {"externalDocs", (o, n) => o.ExternalDocs = LoadExternalDocs(n)}
        };

        public static PatternFieldMap<OpenApiDocument> OpenApiPatternFields = new PatternFieldMap<OpenApiDocument>
        {
            // We have no semantics to verify X- nodes, therefore treat them as just values.
            {s => s.StartsWith("x-"), (o, p, n) => o.AddExtension(p, n.CreateAny())}
        };

        private static void MakeServers(IList<OpenApiServer> servers, ParsingContext context)
        {
            var host = context.GetTempStorage<string>("host");
            var basePath = context.GetTempStorage<string>("basePath");
            var schemes = context.GetTempStorage<List<string>>("schemes");

            if (schemes != null)
            {
                foreach (var scheme in schemes)
                {
                    var server = new OpenApiServer();
                    server.Url = scheme + "://" + (host ?? "example.org/") + (basePath ?? "/");
                    servers.Add(server);
                }
            }
        }

        public static OpenApiDocument LoadOpenApi(RootNode rootNode)
        {
            var openApidoc = new OpenApiDocument();

            var openApiNode = rootNode.GetMap();

            var required = new List<string> {"info", "swagger", "paths"};

            ParseMap(openApiNode, openApidoc, OpenApiFixedFields, OpenApiPatternFields, required);

            ReportMissing(openApiNode, required);

            // Post Process OpenApi Object
            MakeServers(openApidoc.Servers, openApiNode.Context);

            return openApidoc;
        }
    }
}