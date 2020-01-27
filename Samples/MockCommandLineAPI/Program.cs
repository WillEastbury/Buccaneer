using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using static Buccaneer.SwaggerGenerator;

namespace MockAPI
{
    public class Program
    {
        static void Main(string[] args)
        {
            Buccaneer.SwaggerGenerator gen = new Buccaneer.SwaggerGenerator();
            Dictionary<string, string> scopes = new Dictionary<string, string> { { "https://tenant.onmicrosoft.com/tenantcom/connect", "Connect" } };

            Console.Write(gen.GetSwaggerAsYAMLString(
                _ass: Assembly.GetExecutingAssembly(),
                _title: "Wills Blog API",
                _apiversion: "0.1",
                _description: "A sample blog API",
                _contactname: "Will Eastbury",
                _contactemail: "willeastbury@gmail.com",
                _apiServerPaths: new List<string>() {
                    "http://localhost:7071/api", 
                    "https://www.willeastbury.com/api" 
                },
                _oauth2scopes: scopes,
                _headerapikeyname: null,
                _oauth2AuthUrl: "https://tenant.b2clogin.com/tenant.onmicrosoft.com/oauth2/v2.0/authorize?p=B2C_1_willeastbury.com",
                _securitySchemeName: "oauth2"
                )
            );
            Console.ReadLine();
        }  
    }

    public interface ITenantAPI
    {
        [OpenApiTagInputParameter("tenantId", true, "string", "Path")]
        [OpenApiTagMethod("Tenant", "Add Tenant", "Adds a BlogTenant to the platform", new int[] { 200, 404 }, "BlogTenant", "oauth2", "", route: "Tenants/{tenantId}", methodlist: "post")]
        public HttpRequestMessage AddTenant();
    }

    public interface IBlogAPI
    {
        [OpenApiTagInputParameter("tenantId", true, "string", "Path")]
        [OpenApiTagMethod("Blog", "Add Blog", "Adds a Blog to the platform", new int[] { 200, 404 }, "Blog", "oauth2", "", route: "Tenants/{tenantId}/Blogs", methodlist: "post")]
        public HttpRequestMessage AddBlog();
    }
}