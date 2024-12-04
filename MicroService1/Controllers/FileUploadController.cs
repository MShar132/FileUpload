
using Microsoft.AspNetCore.Mvc;
using Dropbox.Api.Common;
using Dropbox.Api.Files;
using Dropbox.Api;
using System.Collections;

namespace MicroService1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly ILogger<FileUploadController> _logger;
        private readonly IConfiguration _settings;
        
        public FileUploadController(ILogger<FileUploadController> logger, IConfiguration setting)
        {
            _logger = logger;
            _settings= setting;
        }

        [Route("GetAuthorizeUri")]
        [HttpGet]


        /// <summary>
        /// opens Dropbox url with code to be further used for OAuth process for the app using its appkey
        /// </summary>
        public ActionResult Get()
        {
            try
            {
                var authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(oauthResponseType: OAuthResponseType.Code,
                                                           clientId: _settings["AppKey"],
                                                           redirectUri: string.Empty,
                                                           state: string.Empty,
                                                           tokenAccessType: TokenAccessType.Offline,
                                                           scopeList: null,
                                                           includeGrantedScopes: IncludeGrantedScopes.User);

                System.Diagnostics.Process.Start(_settings["BrowserForAuthorizeUri"], authorizeUri.ToString());
                return Ok();
            }
            catch
            {

                return StatusCode(204);
            }
            
        }

        /// <summary>
        /// creates dropbox client based on passed-in code and uploads file 
        /// </summary>
        /// <param name="authCode"></param>
        /// <returns></returns>
        [Route("UploadFile")]
        [HttpPost]
        public async Task<ActionResult> UploadFile(string authCode)
        {
            try
            {
                //2nd part of OAuth to obtain token
                var tokenResult = await DropboxOAuth2Helper.ProcessCodeFlowAsync(code: authCode, appKey: _settings["AppKey"],
                                                                                 appSecret: _settings["AppSecret"], redirectUri: string.Empty);
                var client = new DropboxClient(appKey: _settings["AppKey"],
                          appSecret: _settings["AppSecret"],
                          oauth2AccessToken: tokenResult.AccessToken,
                          oauth2RefreshToken: tokenResult.RefreshToken,
                          oauth2AccessTokenExpiresAt: tokenResult.ExpiresAt.Value);
                await client.Files.UploadAsync(_settings["FileName"], WriteMode.Overwrite.Instance, true, null, false, null, false, null, 
                                                                    new MemoryStream(System.Text.UTF8Encoding.UTF8.GetBytes(_settings["FileText"])));
                return Ok();
            }
            catch
            {
                return StatusCode(204);
            }

        }
        /// <summary>
        /// Lists the items within a folder in dropbox
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        [Route("ListFolderContentsInConsole")]
        [HttpGet]
        public async Task<ActionResult> ListFolder(string authCode)
        {
            try
            {
                //2nd part of OAuth to obtain token
                var tokenResult = await DropboxOAuth2Helper.ProcessCodeFlowAsync(code: authCode, appKey: _settings["AppKey"],
                                                                                 appSecret: _settings["AppSecret"], redirectUri: string.Empty);
                var client = new DropboxClient(appKey: _settings["AppKey"],
                          appSecret: _settings["AppSecret"],
                          oauth2AccessToken: tokenResult.AccessToken,
                          oauth2RefreshToken: tokenResult.RefreshToken,
                          oauth2AccessTokenExpiresAt: tokenResult.ExpiresAt.Value);
                // Fetch file/folder list from root
                var folder = await client.Files.ListFolderAsync(string.Empty, false, false, false, false, false, null, null, null, true);
                var fileList = new List<string>();
                foreach (var item in folder.Entries)
                {
                    //if the Entry is a file, append the name to fileList
                    if (item.AsFile != null)
                        fileList.Add(item.AsFile.Name + "\n");
                }

                foreach(var item in fileList)
                {
                    //print file name to console
                    Console.WriteLine(item);
                }
                return Ok();
            }
            catch
            {
                return StatusCode(204);
            }
        }
        
    }
}
