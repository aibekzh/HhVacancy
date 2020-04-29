using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using hh_vacancies.Objects;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Npgsql;
using RestSharp;

namespace hh_vacancies
{
    class Parser
    {
        public static async Task Execute()
        { 
            string[] list =
            {
                "А", "Б", "В", "Г", "Д", "Е", "Ж", "З", "И", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф",
                "Х", "Ц", "Ч", "Ш", "Щ", "Э", "Ю", "Я", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "%23"
            };
            for (int k = 0; k < list.Length; k++)
            {
                for (int i = 0; i < 50; i++)
                    {
                        var address = "https://hh.kz/employers_list?areaId=40&letter=" + list[k] +
                                      "&page=" + i + "";
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine(
                            $"< task : parsing page [{i}] with letter [{list[k]}] '{address}' {DateTime.Now}>");
                        Console.ResetColor();

                        var htmlDoc = await GetHtmlDocument(address);


                        if (htmlDoc.DocumentNode.SelectNodes("//tr/td/div/a") == null)
                        {
                            break;
                        }

                        var htmlNodes = htmlDoc.DocumentNode.SelectNodes("//tr/td/div/a");

                        foreach (var node in htmlNodes)
                        {
                            var link = "https://hh.kz" + node.Attributes["href"].Value;
                            var comp_id = link.Substring(link.LastIndexOf("/") + 1);
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.WriteLine(
                                $"< task : parsing company[{comp_id}] in page [{i}] with letter [{list[k]}] '{link}' {DateTime.Now}>");
                            Console.ResetColor();

                            ParseVacancies(comp_id); //,GetCompDescription(htmlDoc1),comp_web);
                        }
                    }
                
            }
        }
        /*private static string GetCompDescription(HtmlDocument htmlDoc)
        {

            string comp_description = null;

            if (htmlDoc.DocumentNode.SelectSingleNode("//div[@class='g-user-content']") != null)
            {
                var comp_desc = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='g-user-content']")
                    .ChildNodes;

                foreach (var desc in comp_desc)
                {
                    if (desc.ChildNodes != null)
                    {
                        foreach (var VARIABLE in desc.ChildNodes)
                        {
                            comp_description +=
                                VARIABLE.InnerText.Replace("nbsp;", null).Replace("ndash;", null)
                                    .Replace("quot;", null).Replace("&",   null).Replace("laquo;", null)
                                    .Replace("raquo;", null).Trim() + "\n";
                        }
                    }
                    else
                    {
                        comp_description +=
                            desc.InnerText.Replace("nbsp;", null).Replace("ndash;", null)
                                .Replace("quot;", null)
                                .Replace("&", "").Trim() + "\n";
                    }
                }
            }

            if (comp_description == "") comp_description = null;
            

            return comp_description;
        }*/

        private static async Task ParseVacancies(string comp_id) //,string comp_description,string comp_web)
        {
            for (int j = 0;; j++)
            {
                var address =
                    "https://hh.kz/search/vacancy?L_is_autosearch=false&area=40&clusters=true&currency_code=KZT&employer_id=" +
                    comp_id + "&enable_snippets=true&page=" + j + "";
                var htmlDocument =await GetHtmlDocument(address);
                try
                {
                    var links = htmlDocument.DocumentNode.SelectNodes(
                        "//a[@data-qa='vacancy-serp__vacancy-title']");

                    var tasks = new List<Task>();
                    foreach (var vac in links)
                    {
                        var vac_address = vac.Attributes["href"].Value;
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write($"< task : parsing vac '{vac_address}'");
                        
                        tasks.Add(DoRequest(vac_address, comp_id));
                        if (tasks.Count == 10)
                        {
                            await Task.WhenAny(tasks);
                            tasks.RemoveAll(x => x.IsCompleted);
                        }
                    }
                }
                catch (NullReferenceException e)
                {
                    break;
                }
            }
        }

        public static async Task<HtmlDocument> GetHtmlDocument(string address)
        {
            var proxy = await Proxy.GetProxy();
            HttpResponseMessage response;
            var htmlDoc = new HtmlDocument();
            while (true)
            {
                try
                {
                    var handler = new HttpClientHandler();
                    handler.UseCookies = false;
                    handler.UseProxy = true;
                    handler.Proxy =  proxy;
                    using (var httpClient = new HttpClient(handler))
                    {
                        using (var request = new HttpRequestMessage(new HttpMethod("GET"), address))
                        {
                            try
                            {
                                response =  httpClient.SendAsync(request).GetAwaiter().GetResult();
                            }
                            catch (HttpRequestException)
                            {
                                throw new ExternalException();
                            }
                        }
                    }

                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        break;
                    }
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new ExternalException();
                    }

                    break;
                }
                catch (ExternalException e)
                {
                    proxy = await Proxy.GetProxy();
                    //Console.WriteLine(e);
                }
            }

            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            htmlDoc.LoadHtml(WebUtility.HtmlDecode(content));
            return htmlDoc;
        }


        private static async Task DoRequest(string address, string comp_id)
        {
            var htmlDoc2 = await GetHtmlDocument(address);

            string vac_title = null;
            string salary = null;
            string vac_company = null;
            string adress = null;
           
            JArray Responsibilities = new JArray();
            JArray Requirements = new JArray();
            JArray Conditions = new JArray();
            JValue Other = new JValue("");
            var description = new JObject(new JProperty("Responsibilities", Responsibilities),
                new JProperty("Requirements", Requirements), new JProperty("Conditions", Conditions), new JProperty("Other",Other));
            JArray skills = new JArray();
            string contacts_fio = null;
            string contacts_phone = null;
            string contacts_email = null;
            string create_time = null;
            string vac_id = null;
            string experience = null;
            string employment = null;

            vac_id = address.Substring(address.LastIndexOf("/") + 1);

            try
            {
                vac_title = htmlDoc2.DocumentNode.SelectSingleNode("//*[@data-qa='vacancy-title']").InnerText.Trim();
            }
            catch (Exception)
            {
            }

            try
            {
                salary = htmlDoc2.DocumentNode.SelectSingleNode("//*[@class='vacancy-salary']").InnerText.Trim();
            }
            catch (Exception)
            {
            }

            try
            {
                vac_company = htmlDoc2.DocumentNode.SelectSingleNode("//a[@class='vacancy-company-name']/span")
                    .InnerText.Replace("&amp;", "").Replace("&nbsp;", "").Trim();
            }
            catch (Exception)
            {
            }

            try
            {
                adress = htmlDoc2.DocumentNode.SelectSingleNode("//div[@class='vacancy-company-wrapper']//p[last()]")
                    .InnerText;
            }
            catch (Exception)
            {
                adress = null;
            }

            try
            {
                create_time = htmlDoc2.DocumentNode.SelectSingleNode("//*[@class='vacancy-creation-time']").InnerText
                    .Replace("&nbsp;", null).Trim();
                int pFrom = create_time.IndexOf("опубликована") + "опубликована".Length;
                int pTo = create_time.LastIndexOf(" в ");

                create_time = create_time.Substring(pFrom, pTo - pFrom).Trim();
            }
            catch (Exception)
            {
            }


            try
            {
                experience = htmlDoc2.DocumentNode.SelectSingleNode("//span[@data-qa='vacancy-experience']").InnerText;
            }
            catch (Exception)
            {
            }
            try
            {
                employment = htmlDoc2.DocumentNode.SelectSingleNode("//p[@data-qa='vacancy-view-employment-mode']").InnerText;
            }
            catch (Exception)
            {
            }

            try
            {
                string category = null;
                var desc_nodes = htmlDoc2.DocumentNode.SelectSingleNode("//div[@data-qa='vacancy-description']")
                    .ChildNodes;

                foreach (var desc in desc_nodes)
                {
                    if (desc.ChildNodes != null)
                    {
                        if (desc.InnerText == "Обязанности:" || desc.InnerText == "Требования:" ||
                            desc.InnerText == "Условия:")
                        {
                            category = desc.InnerText;
                            continue;
                        }

                        foreach (var VARIABLE in desc.ChildNodes)
                        {
                            if (VARIABLE.InnerText != "")
                            {
                                if (category == "Обязанности:")
                                {
                                    Responsibilities.Add(VARIABLE.InnerText.Replace("quot;", null)
                                        .Replace("&", null).Trim());
                                }
                                else if (category == "Требования:")
                                {
                                    Requirements.Add(VARIABLE.InnerText.Replace("quot;", null)
                                        .Replace("&", null).Trim());
                                }
                                else if (category == "Условия:")
                                {
                                    Conditions.Add(VARIABLE.InnerText.Replace("quot;", null)
                                        .Replace("&", null).Trim());
                                }
                            }
                        }
                    }
                }

                if (Responsibilities.Count == 0 && Requirements.Count==0 && Conditions.Count == 0 )
                {
                    Other.Value = htmlDoc2.DocumentNode.SelectSingleNode("//div[@class='g-user-content']").ParentNode.InnerHtml;
                    
                }
                
            }
            catch (Exception)
            {
            }
            

            try
            {
                var skills_nodes = htmlDoc2.DocumentNode.SelectNodes("//div[@class='bloko-tag-list']/div/div");
                foreach (var node in skills_nodes)
                {
                    skills.Add(node.InnerText.Trim());
                }
            }
            catch (Exception)
            {
            }
            
            try
            {
                contacts_fio = htmlDoc2.DocumentNode.SelectSingleNode("//*[@data-qa='vacancy-contacts__fio']").InnerText
                    .Replace("&amp;", null).Trim();
            }
            catch (Exception)
            {
            }

            try
            {
                var contacts_phones = htmlDoc2.DocumentNode.SelectNodes("//*[@data-qa='vacancy-contacts__phone']");

                foreach (var ph in contacts_phones)
                {
                    contacts_phone += ph.InnerText.Replace("&amp;", null).Replace("&nbsp;", null).Trim() + "\n";
                }
            }
            catch (Exception)
            {
            }


            try
            {
                contacts_email = htmlDoc2.DocumentNode.SelectSingleNode("//*[@data-qa='vacancy-contacts__email']")
                    .InnerText.Replace("&amp;", null).Trim();
            }
            catch (Exception)
            {
            }

            if (vac_title == "") vac_title = null;
            if (salary == "") salary = null;
            if (vac_company == "") vac_company = null;
            if (adress == "") adress = null;
            //if (description == "") description = null;
            if (contacts_fio == "") contacts_fio = null;
            if (contacts_phone == "") contacts_phone = null;
            if (contacts_email == "") contacts_email = null;
            if (create_time == "") create_time = null;
            if (vac_id == "") vac_id = null;
            if (experience == "") experience = null;
            if (employment == "") employment = null;

            using (var conn = new NpgsqlConnection(
                Configuration.ConnectionString)
            )
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    cmd.CommandText =
                        "INSERT INTO " + Configuration.SchemaName + "." + Configuration.TableName +
                        " (vac_title, salary,region, description, skills,contacts_fio,contacts_phone,contacts_email, create_time , vac_id,relevance_date,active,comp_id,experience,employment,source) values (@vac_title, @salary, @region, @description , @skills, @contacts_fio,@contacts_phone,@contacts_email,@create_time,@vac_id,current_timestamp,true,@comp_id,@experience,@employment,@source)  ON CONFLICT (vac_id) DO UPDATE SET vac_title= @vac_title,salary = @salary, region = @region, description = @description, skills = @skills ,contacts_fio = @contacts_fio,contacts_phone= @contacts_phone,contacts_email= @contacts_email, create_time = @create_time,vac_id = @vac_id,relevance_date = current_timestamp,active = true ,comp_id = @comp_id,experience= @experience,employment = @employment, source = @source";
                    cmd.Parameters.AddWithValue("vac_title", (object) vac_title ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("salary", (object) salary ?? DBNull.Value);
                    //cmd.Parameters.AddWithValue("vac_company", (object) vac_company ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("region", (object) adress ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("description", (object) description.ToString() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("skills", (object) skills.ToString() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("contacts_fio", (object) contacts_fio ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("contacts_phone", (object) contacts_phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("contacts_email", (object) contacts_email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("create_time", (object) create_time ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("experience", (object) experience ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("employment", (object) employment ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("vac_id", (object) int.Parse(vac_id) ?? DBNull.Value);
                    // cmd.Parameters.AddWithValue("comp_description", (object) comp_description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("comp_id", long.Parse(comp_id));
                    cmd.Parameters.AddWithValue("source", (object) address ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }
            }

            Console.WriteLine(" title :" + vac_title + " Company :" + vac_company + "  " + DateTime.Now + ">");
            Console.ResetColor();
        }

        public static void CheckForRelevance(string date)
        {
            var GetConfiguration = new ConfigurationBuilder().AddJsonFile("Configuration.json").Build();
            Configuration.ConnectionString = GetConfiguration["ConnectionString"];
            Configuration.SchemaName = GetConfiguration["SchemaName"];
            Configuration.TableName = GetConfiguration["TableName"];
            using (var conn1 = new NpgsqlConnection(Configuration.ConnectionString))
            {
                conn1.Open();
                using (var cmd1 = new NpgsqlCommand())
                {
                    cmd1.Connection = conn1;
                    cmd1.CommandText = "UPDATE " + Configuration.SchemaName + "." + Configuration.TableName +
                                       " SET active = false WHERE relevance_date < TIMESTAMP '" + date +
                                       "' or relevance_date IS NULL;";
                    cmd1.ExecuteNonQuery();
                    cmd1.Parameters.Clear();
                }
            }
        }
    }
}