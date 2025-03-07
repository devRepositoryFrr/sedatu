using ConaviWeb.Data.Repositories;
using ConaviWeb.Model;
using ConaviWeb.Model.Request;
using ConaviWeb.Model.Response;
using ConaviWeb.Commons;
using ConaviWeb.Models;
using ConaviWeb.Services;
using ConaviWeb.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ConaviWeb.Models.AlertsViewModel;

namespace ConaviWeb.Controllers
{
    public class LoginSedatuController : Controller
    {
        private readonly ISecurityRepository _securityRepository;
        private readonly ISecurityTools _securityTools;
        public LoginSedatuController(ISecurityRepository securityRepository, ISecurityTools securityTools)
        {
            _securityRepository = securityRepository;
            _securityTools = securityTools;
        }
        //[Route("Inicio/{nameSystem?}")]
        public async Task<IActionResult> IndexAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index","IndicadoresReunion");
            }
            if (TempData.ContainsKey("Alert"))
                ViewBag.Alert = TempData["Alert"].ToString();
            return View("../Login/LoginSedatu");   
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Auth([FromForm] UserRequest userRequest)
        {
            Response response = new Response();
            string spassword = _securityTools.GetSHA256(userRequest.Password);
            userRequest.Password = spassword;
            userRequest.IdSistema = 5;
            var userResponse = await _securityRepository.GetLoginByCredentials(userRequest);
            if (userResponse != null)
            {
                userResponse.AccessToken = await _securityTools.GetToken(userResponse);
                var modulos = await _securityRepository.GetModules(userResponse.Id);
                if (userResponse.AccessToken != null)
                {
                    HttpContext.Session.SetObject("ComplexObject",modulos);
                    HttpContext.Session.SetString("Token", userResponse.AccessToken);
                    return (RedirectToAction("Index", userResponse.Controller));
                }
                else
                {
                    TempData["Alert"] = AlertService.ShowAlert(Alerts.Danger, "Ocurrio un error, favor de ponerse en contacto con el administrador del sistema");
                    return RedirectToAction("Index");
                }
                
            }
            else
            {
                TempData["Alert"] = AlertService.ShowAlert(Alerts.Danger, "Usuario y/o Contraseña incorrectos");
                return RedirectToAction("Index");
            }
            
        }

        [Authorize]
        [Route("LogOutSedatu")]
        public IActionResult LogOutSedatu()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("Token");
            HttpContext.Session.Remove("ComplexObject");
            return RedirectToAction("Index");
        }

        [Authorize]
        [Route("mainwindow")]
        [HttpGet]
        public IActionResult MainWindow()
        {
            string token = HttpContext.Session.GetString("Token");
            if (token == null)
            {
                return (RedirectToAction("Index"));
            }
            if (Convert.ToInt32(_securityTools.GetUserFromAccessToken(token)) == 0)
            {
                return (RedirectToAction("Index"));
            }
            ViewBag.Message = BuildMessage(token, 50);
            return View();
        }

        public IActionResult Error()
        {
            ViewBag.Message = "An error occured...";
            return View();
        }

        private string BuildMessage(string stringToSplit, int chunkSize)
        {
            var data = Enumerable.Range(0, stringToSplit.Length / chunkSize).Select(i => stringToSplit.Substring(i * chunkSize, chunkSize));
            string result = "The generated token is:";
            foreach (string str in data)
            {
                result += Environment.NewLine + str;
            }
            return result;
        }
    

    // GET: api/Users
    [HttpPost("GetUserByAccessToken")]
        public async Task<IActionResult> GetUserByAccessToken([FromBody] string accessToken)
        {
            Response response = new Response();
            int userId = _securityTools.GetUserFromAccessToken(accessToken);

            if (userId != 0)
            {
                var userResponse = await _securityRepository.GetLoginByUserId(userId);
                userResponse.AccessToken = accessToken;
                response.Success = 1;
                response.Data = userResponse;
                return Ok(response);
            }

            response.Success = 0;
            response.Message = "Token expirado";
            response.Data = null;
            return BadRequest(response);
        }
        [HttpPost]
        public async Task<IActionResult> UpdatePasswordAsync([FromForm] string Password)
        {
            var user = HttpContext.Session.GetObject<UserResponse>("ComplexObject");
            var success = await _securityRepository.UpdatePassword(user.Id, Password);
            if (success)
            {
                TempData["Alert"] = AlertService.ShowAlert(Alerts.Success, "Contraseña actualizada correctamente");
                return RedirectToAction("Index","Home", new { pass = 2 });
            }
            TempData["Alert"] = AlertService.ShowAlert(Alerts.Danger, "Ocurrio un error al actualizar la contraseña, favor de intentarlo nuevamente.");
            return RedirectToAction("Index", "Home");
        }
    }
}
