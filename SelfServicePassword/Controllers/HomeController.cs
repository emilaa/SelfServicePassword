using Microsoft.AspNetCore.Mvc;
using SelfServicePassword.Exceptions;
using SelfServicePassword.ViewModels;
using System.DirectoryServices.AccountManagement;

namespace SelfServicePassword.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult CheckPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CheckPassword(UserVM existUser)
        {
            string domain = _configuration.GetSection("ADConnection:Domain").Value;
            string administrator = _configuration.GetSection("ADConnection:Admin").Value;

            try
            {
                if (existUser.Username != null && existUser.CurrentPassword != null)
                {
                    using (var context = new PrincipalContext(ContextType.Domain,
                        domain, administrator, "Emil123."))
                    {
                        UserPrincipal user = UserPrincipal.FindByIdentity(context, existUser.Username);

                        if (user != null)
                        {
                            user.RefreshExpiredPassword();

                            bool isValid = context.ValidateCredentials(existUser.Username, existUser.CurrentPassword);

                            if (isValid)
                            {
                                HttpContext.Session.SetString("Displayname", user.Name);
                                HttpContext.Session.SetString("Username", user.SamAccountName);
                                return RedirectToAction(nameof(ChangePassword));
                            }
                            else
                            {
                                ViewBag.UsernameOrPasswordError = "İstifadəçi adı və ya şifrə yanlışdır!";
                            }
                        }
                        else
                        {
                            ViewBag.UsernameOrPasswordError = "İstifadəçi adı və ya şifrə yanlışdır!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is PrincipalServerDownException)
                {
                    CustomException exception = new("Serverlə əlaqə qurmaq mümkün olmadı.", ex);
                    ViewBag.Error = exception.Message;
                }
            }

            return View();
        }

        public IActionResult ChangePassword()
        {
            ViewBag.Displayname = HttpContext.Session.GetString("Displayname");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(UserVM existUser)
        {
            string domain = _configuration.GetSection("ADConnection:Domain").Value;
            string administrator = _configuration.GetSection("ADConnection:Admin").Value;
            string? username = HttpContext.Session.GetString("Username");
            string? displayname = HttpContext.Session.GetString("Displayname");

            try
            {
                ViewBag.Displayname = displayname;

                if (existUser.NewPassword != null && existUser.ConfirmPassword != null)
                {
                    if (existUser.NewPassword != existUser.ConfirmPassword)
                    {
                        ViewBag.CheckPassword = "Yeni şifrəniz ilə uyğun deyil.";
                        ViewBag.Displayname = displayname;
                        return View();
                    }

                    using (var context = new PrincipalContext(ContextType.Domain,
                        domain, administrator, "Emil123."))
                    {
                        UserPrincipal user = UserPrincipal.FindByIdentity(context, username);

                        if (user != null)
                        {
                            user.SetPassword(existUser.NewPassword);
                            user.Save();
                            user.SetPassword(existUser.NewPassword);
                            user.Save();

                            ViewBag.Success = "Şifrəniz müvəffəqiyyətlə dəyişdirildi!";
                            ViewBag.Displayname = displayname;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is PasswordException)
                {
                    CustomException exception = new("Şifrəniz tələblərə uyğun deyil.", ex);
                    ViewBag.PasswordError = exception.Message;
                    ViewBag.PasswordInfo = "Daha ətraflı:";
                    ViewBag.PasswordRequirements = "Şifrə tələbləri";
                }

                ViewBag.Displayname = displayname;
            }

            return View();
        }

        public IActionResult PasswordRequirements()
        {
            return View();
        }
    }
}