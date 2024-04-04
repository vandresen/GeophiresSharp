using BlazorGeophiresSharp.Server.Core;
using BlazorGeophiresSharp.Shared;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorGeophiresSharp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ModelingController : Controller
    {
        private readonly ILogger<ModelingController> _logger;
        private readonly ModelCalculation _mc;

        public ModelingController(ILogger<ModelingController> logger, ModelCalculation mc)
        {
            _logger = logger;
            _mc = mc;
        }

        [HttpPost]
        public async Task<Response> Post(InputParameters input)
        {
            Response response = new Response();
            response.IsSuccess = false;
            try
            {
                //string[] lines = input.Content.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                string[] lines = input.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                await _mc.ReadFromRepository(lines);
                _logger.LogInformation("Data read from repository");
                _mc.CalculateModel(input.TempDataContent);
                _logger.LogInformation("Model created");
                string result = await _mc.CreateReport();
                response.Result = result;
                _logger.LogInformation("Report created");
                //_logger.LogInformation(response.Result.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"error getting model: {ex}");

            }
            
            
            return response;
        }
    }
}
