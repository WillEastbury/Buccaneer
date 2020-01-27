using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace Buccaneer
{
    public class SwaggerGenerator
    {
        public HttpResponseMessage GetSwaggerAsHttpResponseMessageWithYAMLString(Assembly _ass, string _title, string _apiversion, string _description, string _contactname, string _contactemail, List<string> _apiServerPaths, Dictionary<string, string> _oauth2scopes, string _headerapikeyname, string _oauth2AuthUrl, string _securitySchemeName = "oauth2")
        {
            OpenApiDocument document = GetOpenApiDocument(_ass, _title, _apiversion, _description, _contactname, _contactemail, _apiServerPaths, _oauth2scopes, _oauth2AuthUrl, _securitySchemeName);
            return new HttpResponseMessage
            {
                Content = new StringContent(document.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Yaml))
            };
        }
        public HttpResponseMessage GetSwaggerAsHttpResponseMessageWithJSONString(Assembly _ass, string _title, string _apiversion, string _description, string _contactname, string _contactemail, List<string> _apiServerPaths, Dictionary<string, string> _oauth2scopes, string _headerapikeyname, string _oauth2AuthUrl, string _securitySchemeName = "oauth2")
        {
            OpenApiDocument document = GetOpenApiDocument(_ass, _title, _apiversion, _description, _contactname, _contactemail, _apiServerPaths, _oauth2scopes, _oauth2AuthUrl, _securitySchemeName);
            return new HttpResponseMessage
            {
                Content = new StringContent(document.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json))
            };
        }
        public string GetSwaggerAsYAMLString(Assembly _ass, string _title, string _apiversion, string _description, string _contactname, string _contactemail, List<string> _apiServerPaths, Dictionary<string, string> _oauth2scopes, string _headerapikeyname, string _oauth2AuthUrl, string _securitySchemeName = "oauth2")
        {
            return GetOpenApiDocument(_ass, _title, _apiversion, _description, _contactname, _contactemail, _apiServerPaths, _oauth2scopes, _oauth2AuthUrl, _securitySchemeName).Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Yaml);
        }
        public string GetSwaggerAsJSONString(Assembly _ass, string _title, string _apiversion, string _description, string _contactname, string _contactemail, List<string> _apiServerPaths, Dictionary<string, string> _oauth2scopes, string _headerapikeyname, string _oauth2AuthUrl, string _securitySchemeName = "oauth2")
        {
            return GetOpenApiDocument(_ass, _title, _apiversion, _description, _contactname, _contactemail, _apiServerPaths, _oauth2scopes, _oauth2AuthUrl, _securitySchemeName).Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
        }
        public OpenApiDocument GetOpenApiDocument(Assembly _ass, string _Title, string _apiversion, string _description, string _contactname, string _contactemail, List<string> _apiServerPaths, Dictionary<string, string> oauth2scopes, string oauth2AuthUrl, string securitySchemeName = "oauth2")
        {
            // Kick off the document creation
            OpenApiDocument document = new OpenApiDocument
            {
                Info = new OpenApiInfo()
                {

                    Title = _Title,
                    Version = _apiversion,
                    Description = _description,
                    Contact = new OpenApiContact()
                    {
                        Name = _contactname,
                        Email = _contactemail
                    }
                },
                Servers = new List<OpenApiServer>(),
                Paths = new OpenApiPaths() { },
                Components = new OpenApiComponents()
                {
                    SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>(),
                    Schemas = new Dictionary<string, OpenApiSchema>()
                },
                SecurityRequirements = new List<OpenApiSecurityRequirement>()
            };

            foreach (string apiserver in _apiServerPaths)
            {
                document.Servers.Add(new OpenApiServer { Url = apiserver });
            }

            // Security Setup
            OpenApiSecurityScheme ss = new OpenApiSecurityScheme()
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows()
                {
                    Implicit = new OpenApiOAuthFlow()
                    {
                        AuthorizationUrl = new Uri(oauth2AuthUrl),
                        Scopes = oauth2scopes

                    }
                },
                Reference = new OpenApiReference()
                {
                    Id = securitySchemeName,
                    Type = ReferenceType.SecurityScheme
                }
            };

            OpenApiSecurityRequirement sr = new OpenApiSecurityRequirement();
            sr.Add(ss, new List<string>() { oauth2scopes.FirstOrDefault().Key });
            //document.SecurityRequirements.Add(sr);
            document.Components.SecuritySchemes.Add(securitySchemeName, ss);

            // Lets get the core methods from the executing assembly and figure out what the APIDef should look like
            var functionAssembly = _ass;

            var tags = functionAssembly.GetTypes()
               .SelectMany(t => t.GetMethods())
               .Where(m => m.GetCustomAttributes(typeof(OpenApiTagMethodAttribute), false).Any())
               .Select(e => e.GetCustomAttributes(typeof(OpenApiTagMethodAttribute), false))
               .ToList();

            var methods = functionAssembly.GetTypes()
               .SelectMany(t => t.GetMethods())
               .Where(m => m.GetCustomAttributes(typeof(OpenApiTagMethodAttribute), false).Any())
               .ToList();

            List<string> tagnames = new List<string>() { "Default" };

            // Search for the different tags
            foreach (object att in tags)
            {
                // Add it if it's a new one we havent seen yet
                OpenApiTagMethodAttribute opentag = ((OpenApiTagMethodAttribute[])att).First();
                if (!tagnames.Contains(opentag.TagName))
                {
                    tagnames.Add(opentag.TagName);

                    document.Tags.Add(new OpenApiTag() { Name = opentag.TagName, Description = opentag.TagName });
                };

                // Loop through them and figure out what the trigger looks like.
                foreach (MethodInfo methodInfo in methods)
                {
                    OpenApiTagMethodAttribute tagAttribute = null;
                    OpenApiTagInputParameterAttribute[] parameterAttributes = null;

                    //foreach (ParameterInfo parameter in methodInfo.GetParameters())
                    //{
                    tagAttribute = (OpenApiTagMethodAttribute)methodInfo.GetCustomAttribute(typeof(OpenApiTagMethodAttribute), false);
                    parameterAttributes = (OpenApiTagInputParameterAttribute[])methodInfo.GetCustomAttributes(typeof(OpenApiTagInputParameterAttribute), false);

                    foreach (string verb in tagAttribute.Methodlist)
                    {
                        if (tagAttribute != null)
                        {
                            var resp = new OpenApiResponses();

                            foreach (int resp1 in tagAttribute.ResponseCodes)
                            {
                                if (tagAttribute.ResponseClass != "string" && tagAttribute.ResponseClass != "")
                                {
                                    CheckSchemaAndCreateIfNotExists(document, tagAttribute.ResponseClass, tagAttribute.ResponseClass);
                                    Dictionary<string, OpenApiMediaType> mtd = new Dictionary<string, OpenApiMediaType>();
                                    mtd.Add("application/json", new OpenApiMediaType
                                    {
                                        Schema = document.Components.Schemas[tagAttribute.ResponseClass]
                                    });

                                    resp.Add(resp1.ToString(), new OpenApiResponse()
                                    {
                                        // HACK: We should really change the description of the response according to the response type here
                                        // e.g. 201 Should return "Created", 409 should send back "Conflict, that item already exists" etc etc.
                                        // Maybe even make the int an enum too to limit the possible response values, or pass a tuple with the code and text

                                        Description = $"{resp1}: OK",
                                        Content = mtd,

                                    });
                                }
                                else
                                {
                                    Dictionary<string, OpenApiMediaType> mtd2 = new Dictionary<string, OpenApiMediaType>();
                                    mtd2.Add("text/plain", new OpenApiMediaType
                                    {

                                    });

                                    resp.Add(resp1.ToString(), new OpenApiResponse()
                                    {
                                        // HACK: We should really change the description of the response according to the response type here
                                        // e.g. 201 Should return "Created", 409 should send back "Conflict, that item already exists" etc etc.
                                        // Maybe even make the int an enum too to limit the possible response values, or pass a tuple with the code and text

                                        Description = $"{resp1}: OK",
                                        Content = mtd2,

                                    });
                                }
                            };

                            if (tagAttribute.AuthScheme != "")
                            {

                                resp.Add("401", new OpenApiResponse()
                                {
                                    Description = "Not Authorized"
                                });

                                resp.Add("403", new OpenApiResponse()
                                {
                                    Description = "Forbidden"
                                });
                            }

                            if (!document.Paths.ContainsKey("/" + tagAttribute.Route))
                            {
                                document.Paths.Add("/" + tagAttribute.Route, new OpenApiPathItem
                                {
                                    Operations = new Dictionary<OperationType, OpenApiOperation>(),
                                });
                            };

                            List<OpenApiParameter> lpar = new List<OpenApiParameter>();

                            foreach (OpenApiTagInputParameterAttribute apiparam in parameterAttributes)
                            {
                                if (tagAttribute.ResponseClass != "" && tagAttribute.ResponseClass != "string") CheckSchemaAndCreateIfNotExists(document, tagAttribute.ResponseClass, tagAttribute.ResponseClass);

                                lpar.Add(new OpenApiParameter()
                                {
                                    Name = apiparam.ParameterName,
                                    Required = apiparam.IsRequired,
                                    In = (ParameterLocation)Enum.Parse(typeof(Microsoft.OpenApi.Models.ParameterLocation), apiparam.Location, true),
                                    Schema = new OpenApiSchema()
                                    {
                                        Type = apiparam.TypeName
                                    }
                                });
                            }

                            // Is there a body parameter in here somewhere ? 
                            if (tagAttribute.BodyObjectClass != null && tagAttribute.BodyObjectClass != "")
                            {
                                CheckSchemaAndCreateIfNotExists(document, tagAttribute.BodyObjectClass, tagAttribute.BodyObjectClass);
                                Dictionary<string, OpenApiMediaType> rbt = new Dictionary<string, OpenApiMediaType>();
                                rbt.Add("application/json", new OpenApiMediaType()
                                {
                                    Schema = document.Components.Schemas[tagAttribute.BodyObjectClass]
                                });

                                // Each of these should become an action in the operations dictionary that we are tagged with
                                document.Paths["/" + tagAttribute.Route].AddOperation((OperationType)Enum.Parse(typeof(OperationType), verb.ToString(), true), new OpenApiOperation()
                                {
                                    Description = tagAttribute.Description,
                                    Summary = tagAttribute.Summary,
                                    Parameters = lpar,
                                    RequestBody = new OpenApiRequestBody()
                                    {
                                        Content = rbt,
                                        Required = true,
                                        Description = tagAttribute.BodyObjectClass + " object"
                                    },
                                    Tags = new[] { new OpenApiTag() {
                                            Name = tagAttribute.TagName }}.ToList(),
                                    Responses = resp
                                });

                            }
                            else
                            {
                                CheckSchemaAndCreateIfNotExists(document, tagAttribute.BodyObjectClass, tagAttribute.BodyObjectClass);
                                // Each of these should become an action in the operations dictionary that we are tagged with
                                document.Paths["/" + tagAttribute.Route].AddOperation((OperationType)Enum.Parse(typeof(OperationType), verb.ToString(), true), new OpenApiOperation()
                                {
                                    Description = tagAttribute.Description,
                                    Summary = tagAttribute.Summary,
                                    Parameters = lpar,
                                    Tags = new[] { new OpenApiTag() {
                                            Name = tagAttribute.TagName }}.ToList(),
                                    Responses = resp
                                });

                            }

                            if (tagAttribute.AuthScheme != "")
                            {
                                List<OpenApiSecurityRequirement> srl = new List<OpenApiSecurityRequirement>();
                                srl.Add(sr);
                                document.Paths["/" + tagAttribute.Route].Operations[(OperationType)Enum.Parse(typeof(OperationType), verb.ToString(), true)].Security = srl;

                            }
                        }
                    }
                    //}
                };
            }

            return document;
        }
        private static string CheckSchemaAndCreateIfNotExists(OpenApiDocument document, string objectStringKey, string title = "")
        {
            string mappedto = null;
            if (objectStringKey != null)
            {

                switch (objectStringKey.ToLower())
                {
                    case "":
                        break;

                    case "string":
                        mappedto = "string";
                        break;

                    case "integer":
                        mappedto = "number";
                        break;

                    case "int32":
                        mappedto = "number";
                        break;

                    case "int64":
                        mappedto = "number";
                        break;

                    case "array":
                        mappedto = "array";
                        break;

                    case "boolean":
                        mappedto = "boolean";
                        break;

                    case "number":
                        mappedto = "number";
                        break;

                    case "float":
                        mappedto = "number";
                        break;

                    case "date":
                        mappedto = "string";
                        break;

                    case "datetime":
                        mappedto = "string";
                        break;

                    case "file":
                        mappedto = "string";
                        break;

                    case "byte[]":
                        mappedto = "string";
                        break;

                    case "email":
                        mappedto = "string";
                        break;

                    case "uuid":
                        mappedto = "string";
                        break;

                    case "double":
                        mappedto = "number";
                        break;

                    case "list`1":
                        mappedto = "array";
                        break;

                    case "dictionary`2":
                        mappedto = "array";
                        break;

                    case "uri":
                        mappedto = "string";
                        break;

                    case "idisposable":
                        mappedto = "exclude";
                        break;

                    case "t":
                        mappedto = "exclude";
                        break;

                    case "object":
                        mappedto = "exclude";
                        break;

                    case "tvalue":
                        mappedto = "exclude";
                        break;

                    default:
                        mappedto = "object";
                        if (!document.Components.Schemas.Keys.Contains(objectStringKey))
                        {
                            document.Components.Schemas.Add(objectStringKey, CreateDefinitionSchema(document, objectStringKey, title));
                        }
                        break;
                }
            }

            return mappedto;
        }

        private static OpenApiSchema CreateDefinitionSchema(OpenApiDocument doc, string TypeName, string title, string map = "object")
        {
            // Gets called recursively to walk the schema and create downlevel definitions.
            OpenApiSchema schema = new OpenApiSchema()
            {
                Type = "object",
                Title = title,
                Properties = new Dictionary<string, OpenApiSchema>(),
                Required = new SortedSet<string>(),
                AdditionalPropertiesAllowed = false,
                Reference = new OpenApiReference()
                {
                    Type = ReferenceType.Schema,
                    Id = TypeName
                }
            };

            Assembly functionAssembly = Assembly.GetExecutingAssembly();
            Type ThisType = functionAssembly.GetType(TypeName);
            if (ThisType == null)
            {
                // Note that this kind of search is extremely slow, but is the only way to find the proper type we want if we don't have a fully resolved name 
                // so since we won't be calling it very often - let's go with it for simplicity.

                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type typ in a.GetTypes())
                    {
                        // Check for this being the type we want.
                        if (typ.Name == TypeName)
                        {
                            ThisType = typ;
                            break;
                        }
                    }
                    if (ThisType != null)
                    {
                        break;
                    }
                }
            }

            if (ThisType != null)
            {
                foreach (PropertyInfo property in ThisType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    // Edge case #5,000,001, the property is an enum
                    if (property.PropertyType.IsEnum)
                    {
                        schema.Properties.Add(property.Name, new OpenApiSchema()
                        {
                            Type = "array",
                            Items = new OpenApiSchema()
                            {
                                Type = "string"
                            }
                        });

                        foreach (object enumitem in Enum.GetValues(property.PropertyType))
                        {
                            schema.Properties[property.Name].Enum.Add(new OpenApiString(enumitem.ToString()));
                        }

                    }


                    // Edge Case test #2, you have a property that is a generic e.g. a list<T> so we need to include the def of T in the output
                    if (property.PropertyType.GenericTypeArguments.Length != 0)
                    {
                        foreach (Type targ in property.PropertyType.GetGenericArguments())
                        {
                            if (property.PropertyType.Name == "List`1")
                            {
                                // What is this a list of, another nested object or something else, a List<string> perhaps ?
                                if (!schema.Properties.ContainsKey(targ.Name))
                                {
                                    string news = CheckSchemaAndCreateIfNotExists(doc, targ.Name, targ.Name);
                                    if (news.ToLower() != "object")
                                    {
                                        // this is a simple list of a primitive type
                                        schema.Properties.Add(property.Name, new OpenApiSchema()
                                        {
                                            Type = "array",
                                            Items = new OpenApiSchema()
                                            {
                                                Type = news,
                                                Title = targ.Name
                                            }
                                        });
                                    }
                                    else
                                    {
                                        // This is a list of a type that needs to be added and referenced
                                        schema.Properties.Add(property.Name, new OpenApiSchema()
                                        {
                                            Type = "array",
                                            Items = doc.Components.Schemas[targ.Name]
                                        });
                                    }
                                }
                            }

                            else
                            {
                                if (property.PropertyType.Name == "Dictionary`2")
                                {
                                    // Edge Case 1 Serializing a property of a dictionary<T,K>

                                    if (!schema.Properties.ContainsKey(property.Name))
                                        schema.Properties.Add(property.Name, new OpenApiSchema()
                                        {
                                            Type = "object",
                                            AdditionalProperties = new OpenApiSchema()
                                            {

                                                Type = "object"

                                            }
                                        });


                                }
                                else
                                {
                                    // Edge case 3, it's a generic but not a list<T>
                                    if (!schema.Properties.ContainsKey(targ.Name))
                                    {
                                        schema.Properties.Add(targ.Name, new OpenApiSchema()
                                        {
                                            Type = CheckSchemaAndCreateIfNotExists(doc, targ.Name, targ.Name),
                                            Format = targ.Name

                                        });
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // This is a normal property, which might be a nested object, it might not be
                        Console.WriteLine($"Discovered non-generic Object Property {property.Name}, of type {property.PropertyType.Name}");
                        if (!schema.Properties.ContainsKey(property.Name))
                        {
                            schema.Properties.Add(property.Name, new OpenApiSchema()
                            {
                                Type = CheckSchemaAndCreateIfNotExists(doc, property.PropertyType.Name, property.PropertyType.Name),
                                Format = property.PropertyType.Name
                            });
                        }
                    }
                }
            }
            else
            {
                // Edge #4 - Type doesn't seem to exist anywhere
                Console.WriteLine($">>> Type not found in any assemblies! {TypeName}");
            }

            return schema;
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]

        public class OpenApiTagInputParameterAttribute : Attribute
        {
            public OpenApiTagInputParameterAttribute(string ParameterName, bool IsRequired, string TypeName, string Location)
            {

                if (string.IsNullOrEmpty(ParameterName))
                {
                    throw new ArgumentException("message", nameof(ParameterName));
                }

                if (string.IsNullOrEmpty(TypeName))
                {
                    throw new ArgumentException("message", nameof(TypeName));
                }

                this.ParameterName = ParameterName;
                this.IsRequired = IsRequired;
                this.TypeName = TypeName;
                this.Location = Location;

            }

            public string ParameterName { get; }
            public bool IsRequired { get; }
            public string TypeName { get; }
            public string Location { get; }
        }

        public class OpenApiTagMethodAttribute : Attribute
        {
            public OpenApiTagMethodAttribute(string tagName, string description, string summary, int[] responseCodes, string responseClass, string authscheme, string bodyObjectClass, string route, params string[] methodlist)
            {

                if (string.IsNullOrEmpty(tagName))
                {
                    throw new ArgumentException("message", nameof(tagName));
                }

                TagName = tagName;
                Summary = summary;
                Description = description;
                ResponseCodes = responseCodes;
                ResponseClass = responseClass;
                BodyObjectClass = bodyObjectClass;
                AuthScheme = authscheme;
                Methodlist = methodlist;
                Route = route;

            }

            public string TagName { get; set; }
            public string Description { get; set; }
            public string Summary { get; set; }
            public int[] ResponseCodes { get; set; }
            public string ResponseClass { get; set; }
            public string BodyObjectClass { get; set; }
            public string AuthScheme { get; set; }
            public string[] Methodlist { get; set; }
            public string Route { get; set; }
        }

        public enum httpVerb
        {
            get,
            post,
            delete,
            head,
            patch,
            put,
            options
        }
    }
}
