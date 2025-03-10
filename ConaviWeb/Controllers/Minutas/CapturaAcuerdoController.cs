﻿using ConaviWeb.Data.Minuta;
using ConaviWeb.Data.Repositories;
using ConaviWeb.Model;
using ConaviWeb.Model.Minuta;
using ConaviWeb.Services;
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
    public class CapturaAcuerdoController : Controller
    {
        private readonly IMinutaRepository _minutaRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IUserRepository _userRepository;
        public CapturaAcuerdoController(IMinutaRepository minutaRepository, IWebHostEnvironment environment, IUserRepository userRepository)
        {
            _minutaRepository = minutaRepository;
            _environment = environment;
            _userRepository = userRepository;
        }
        public async Task<IActionResult> IndexAsync()
        {
            var reuniones = await _minutaRepository.GetReunion();
            var responsable = await _minutaRepository.GetResponsable();
            var gestion = await _minutaRepository.GetGestion();
            var estatus = await _minutaRepository.GetEstatus();
            ViewData["Responsable"] = responsable;
            ViewData["Gestion"] = gestion;
            ViewData["Reuniones"] = reuniones;
            ViewData["Estatus"] = estatus;
            if (TempData.ContainsKey("Alert"))
                ViewBag.Alert = TempData["Alert"].ToString();
            return View("../Minuta/CapturaAcuerdo");
        }
        [HttpPost]
        public async Task<IActionResult> CreateAcuerdoAsync(Acuerdo acuerdo)
        {
            User user = await _userRepository.GetUserDetails(Int32.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
            acuerdo.CreatedBy = user.Email;
            var success = await _minutaRepository.InsertAcuerdo(acuerdo);
            if (!success)
            {
                TempData["Alert"] = AlertService.ShowAlert(Alerts.Danger, "Ocurrio un error al registrar el acuerdo");
                return RedirectToAction("Index");
            }
            TempData["Alert"] = AlertService.ShowAlert(Alerts.Success, "Se agrego el acuerdo con exito");
            return RedirectToAction("Index");

        }
        [Route("DeleteAcuerdo/{id?}/idReunion")]
        public async Task<IActionResult> DeleteAcuerdoAsync(int id, int idReunion)
        {
            var success = await _minutaRepository.DeleteAcuerdo(id);
            if (!success)
            {
                TempData["Alert"] = AlertService.ShowAlert(Alerts.Danger, "Ocurrio un error al borrar el acuerdo");
                return RedirectToAction("Index");
            }
            TempData["Alert"] = AlertService.ShowAlert(Alerts.Success, "Se elimino el acuerdo con exito");
            return RedirectToAction("DetalleReunion", "Reunion", new { id = idReunion });

        }
    }
}
