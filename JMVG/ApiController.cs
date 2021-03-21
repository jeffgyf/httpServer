using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Commons;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JMVG
{
    public class ApiController:ControllerBase
    {
        private JmvgDbContext dbContext;
        private string sqlUsername;
        private string sqlPassword;

        public ApiController()
        {
            var config = JsonConvert.DeserializeObject<JToken>(File.ReadAllText("./appsettings.json"));
            sqlUsername = config["Database"]["Username"].ToString();
            sqlPassword = config["Database"]["Password"].ToString();
        }

        [Route("/api/getVideoList")]
        [GetMethod]
        public async Task<IHttpResponse> GetVideoList(HttpRequest request)
        {
            var args = request.Uri.Split('?')[1].Split('&').ToDictionary(s => s.Split('=')[0], s => s.Split('=')[1]);
            int count = int.Parse(args["count"]);
            int start = int.Parse(args["start"]);
            var dbContext = new JmvgDbContext(sqlUsername, sqlPassword);
            var videos = await dbContext.Videos.FromSqlRaw($"SELECT TOP({count}) * FROM [Video] WHERE VideoId >={start}").ToListAsync();
            var response = new HttpStringResponse { Body = JsonConvert.SerializeObject(videos), ContentType = "application/json", Status = 200, Header= "Access-Control-Allow-Origin:*\r\n" };
            return response;
        }

        [Route("api/test")]
        [GetMethod]
        public Task<IHttpResponse> Test(HttpRequest request)
        {
            /*
            var sampleVideo = new Video
            {
                Title = "Become Wind",
                CoverImg = $"https://{HttpContext.Request.Host}/image/SampleCover.png",
                VideoPath = $"https://{HttpContext.Request.Host}/video/become_wind.mp4",
                VideoId = 123
            };
            return Content(JsonConvert.SerializeObject(sampleVideo), "application/json");//*/
            return null;
        }
    }
}
