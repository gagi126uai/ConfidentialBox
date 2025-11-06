using Microsoft.AspNetCore.Http;

namespace ConfidentialBox.Infrastructure.Services;

public interface IClientContextResolver
{
    ClientContext Resolve(HttpContext context);
}
