using EcoFashionBackEnd.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace EcoFashionBackEnd.Settings;


public static class GenerateJsonTokenString
{
   public static string GenerateJsonWebToken(this User user, string secretKey, DateTime now)
   {
       return GenerateJsonWebTokenWithRoleData(user, secretKey, now, null, null);
   }


   public static string GenerateJsonWebTokenWithRoleData(this User user, string secretKey, DateTime now, Supplier? supplier, Designer? designer)
   {
       var sercurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
       var credentials = new SigningCredentials(sercurityKey, SecurityAlgorithms.HmacSha256);


       var claimsList = new List<Claim>
       {
           new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
           new Claim(ClaimTypes.Name, user.FullName ?? ""),
           new Claim(ClaimTypes.Email, user.Email ?? ""),
           new Claim(ClaimTypes.MobilePhone, user.Phone ?? ""),
           new Claim("username", user.Username ?? ""),
           new Claim(ClaimTypes.Role, user.UserRole?.RoleName ?? ""),
           new Claim("roleId", user.RoleId.ToString()),
           new Claim("status", user.Status.ToString()),
           new Claim("createdAt", user.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")),
       };


       // Add SupplierId claim if supplier data is provided
       if (supplier != null)
       {
           claimsList.Add(new Claim("SupplierId", supplier.SupplierId.ToString()));
       }


       // Add DesignerId claim if designer data is provided
       if (designer != null)
       {
           claimsList.Add(new Claim("DesignerId", designer.DesignerId.ToString()));
       }


       var token = new JwtSecurityToken(
           claims: claimsList,
           expires: now.AddDays(7),
           signingCredentials: credentials
       );
       return new JwtSecurityTokenHandler().WriteToken(token);
   }
}



