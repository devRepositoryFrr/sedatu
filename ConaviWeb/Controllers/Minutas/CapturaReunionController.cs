using ConaviWeb.Data.Minuta;
using ConaviWeb.Data.Repositories;
using ConaviWeb.Model;
using ConaviWeb.Model.Minuta;
using ConaviWeb.Services;
using MailKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static ConaviWeb.Models.AlertsViewModel;

namespace ConaviWeb.Controllers.Minutas
{
    [Authorize]
    public class CapturaReunionController : Controller
    {
        private readonly IMinutaRepository _minutaRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IUserRepository _userRepository;
        public CapturaReunionController(IMinutaRepository minutaRepository, IWebHostEnvironment environment, IUserRepository userRepository)
        {
            _minutaRepository = minutaRepository;
            _environment = environment;
            _userRepository = userRepository;
        }
        public async Task<IActionResult> IndexAsync()
        {
            var sector = await _minutaRepository.GetSector();
            var entidad = await _minutaRepository.GetEntidad();
            //var municipio = await _minutaRepository.GetMunicipio();
            var responsable = await _minutaRepository.GetResponsable();
            var gestion = await _minutaRepository.GetGestion();
            var estatus = await _minutaRepository.GetEstatus();
            ViewData["Sector"] = sector;
            ViewData["Entidad"] = entidad;
            //ViewData["Municipio"] = municipio;
            ViewData["Responsable"] = responsable;
            ViewData["Gestion"] = gestion;
            ViewData["Estatus"] = estatus;
            if (TempData.ContainsKey("Alert"))
                ViewBag.Alert = TempData["Alert"].ToString();
            return View("../Minuta/CapturaReunion");
        }

        public async Task<IActionResult> CrearCapturaAsync(Reunion reunion)
        {
            User user = await _userRepository.GetUserDetails(Int32.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
            reunion.CreatedBy = user.Email;
            var success = await _minutaRepository.InsertReunion(reunion);
            if (!success)
            {
                TempData["Alert"] = AlertService.ShowAlert(Alerts.Danger, "Ocurrio un error al guardar la reunión");
                return RedirectToAction("Index");
            }
            TempData["Alert"] = AlertService.ShowAlert(Alerts.Success, "Se guardo la reunión con exito");
            return RedirectToAction("Index");
        }
        [HttpPost, ActionName("GetMunicipios")]
        public async Task<JsonResult> GetMunByIdAsync(string clave)
        {
            IEnumerable<Catalogo> municipio = new List<Catalogo>();
            if (!string.IsNullOrEmpty(clave))
            {
                municipio = await _minutaRepository.GetMunicipio(clave);
            }
            return Json(municipio);
        }
    }
}
