using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using hh_resume.Objects;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Npgsql;
using RestSharp;

namespace hh_resume
{
    public class Parser
    {
        public static void Execute(int j)
        {
            for (int i = 1;i<=250; i++)
            {
                if (i % 5 == j)
                {
                    var htmlDoc = GetHtmlDocument($"https://hh.kz/search/resume?area=40&order_by=relevance&page={i}");
                    var resNodes = htmlDoc.DocumentNode.SelectNodes("//div[@itemscope='itemscope']");

                    foreach (var node in resNodes)
                    {
                        var hash = node.Attributes["data-hh-resume-hash"].Value;
                        var url = $"https://hh.kz/resume/{hash}?hhtmFrom=resume_search_result";
                        var resume_id = node.Attributes["data-resume-id"].Value;
                        Parseresume(url, resume_id);
                    }
                }
            }
        }

        private static void Parseresume(string url, string resume_id)
        {
            var htmlDoc = GetHtmlDocument(url);

            string gender = null;
            string age = null;
            string address = null;
            string job = null;
            string salary = null;
            string general_exp = null;
            JArray work_for = new JArray();
            JValue work_interval = new JValue(""); ////div[@itemprop='worksFor']/div/div[1]
            JValue work_place = new JValue(""); ////div[@itemprop='worksFor']//div[@class='resume-block__sub-title'][1]
            JValue work_pos = new JValue(""); ////div[@itemprop='worksFor']//div[@class='resume-block__sub-title'][2]
            JArray skills = new JArray();
            try
            {
                gender = htmlDoc.DocumentNode.SelectSingleNode("//span[@itemprop='gender']").InnerText.Trim();
            }
            catch (Exception)
            {
            }

            try
            {
                age = htmlDoc.DocumentNode.SelectSingleNode("//span[@data-qa='resume-personal-age']").InnerText.Trim();
            }
            catch (Exception)
            {
            }

            try
            {
                address = htmlDoc.DocumentNode.SelectSingleNode("//span[@data-qa='resume-personal-address']").InnerText
                    .Trim();
            }
            catch (Exception)
            {
            }

            try
            {
                job = htmlDoc.DocumentNode.SelectSingleNode("//span[@data-qa='resume-block-title-position']").InnerText
                    .Trim();
            }
            catch (Exception)
            {
            }

            try
            {
                salary = htmlDoc.DocumentNode.SelectSingleNode("//span[@data-qa='resume-block-salary']").InnerText
                    .Trim();
            }
            catch (Exception)
            {
            }

            try
            {
                general_exp = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(.,'Опыт работы')]").InnerText
                    .Trim();
            }
            catch (Exception)
            {
            }

            try
            {
                var skill_nodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='bloko-tag-list']//span/span");
                foreach (var node in skill_nodes)
                {
                    skills.Add(node.InnerText.Trim());
                }
            }
            catch (Exception)
            {
            }

            try
            {
                var work_nodes = htmlDoc.DocumentNode.SelectNodes("//div[@itemprop='worksFor']");
                foreach (var node in work_nodes)
                {
                    work_interval.Value = node.SelectSingleNode("./div/div[1]").InnerText;
                    work_place.Value = node.SelectSingleNode(".//div[@class='resume-block__sub-title'][1]").InnerText;
                    work_pos.Value = node.SelectSingleNode(".//div[@class='resume-block__sub-title'][2]").InnerText;
                    var description = new JObject(new JProperty("work_interval", work_interval),
                        new JProperty("work_place", work_place), new JProperty("work_pos", work_pos));
                    work_for.Add(description);
                }
            }
            catch (Exception)
            {
            }

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
                        " (resume_id, gender,age, address, job,salary,general_exp,work_for, skills , url,relevance_date) values (@resume_id, @gender,@age, @address, @job, @salary, @general_exp, @work_for, @skills , @url ,current_timestamp)  ON CONFLICT (resume_id) DO UPDATE SET resume_id= @resume_id, gender = @gender,age = @age, address= @address, job= @job,salary = @salary,general_exp =  @general_exp, work_for= @work_for,skills=  @skills , url = @url,relevance_date = current_timestamp";
                    cmd.Parameters.AddWithValue("resume_id", (object) long.Parse(resume_id) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("gender", (object) gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("age", (object) age ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("address", (object) address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("job", (object) job ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("salary", (object) salary ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("general_exp", (object) general_exp ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("work_for", (object) work_for.ToString() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("skills", (object) skills.ToString() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("url", (object) url ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }
            }

            Console.WriteLine(" title :" + work_pos + "  " + DateTime.Now + ">");
            Console.ResetColor();
        }

        public static HtmlDocument GetHtmlDocument(string address)
        {
            var proxy = Proxy.GetProxy();
            HttpResponseMessage response;
            var htmlDoc = new HtmlDocument();
            while (true)
            {
                try
                {
                    var handler = new HttpClientHandler();
                    handler.UseCookies = false;
                    handler.UseProxy = true;
                    handler.Proxy = proxy;
                    using (var httpClient = new HttpClient(handler))
                    {
                        using (var request = new HttpRequestMessage(new HttpMethod("GET"), address))
                        {
                            try
                            {
                                response = httpClient.SendAsync(request).GetAwaiter().GetResult();
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
                    //Console.WriteLine(e);
                }
            }

            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            htmlDoc.LoadHtml(WebUtility.HtmlDecode(content));
            return htmlDoc;
        }
    }
}