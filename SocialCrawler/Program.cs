﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SocialCrawler
{
    public class SocialTableEntry : TableEntity
    {
        public SocialTableEntry(string dataType, string date)
        {
            this.PartitionKey = dataType;
            this.RowKey = date;
        }
        public SocialTableEntry() { }
        public string Count { get; set; }
    }
    class Program
    {
        static string _storageAccountName = "{your azure storage name}";
        static string _storageAccessKey = "{your azure storage key}";
        static string _storageTableName = "{your storage table name}";

        static string _twitterUrl = "https://twitter.com/_mspjp";

        //Facebook Access Token
        //more info => https://wayohoo.com/facebook/tips/how-to-get-an-access-token-of-the-facebook-api.html
        static string _facebookAccessToken = "{your facebook access token}";
        static string _facebookUrl = "https://graph.facebook.com/mspjp?access_token=" + _facebookAccessToken;


        static void Main(string[] args)
        {
            try
            {
                
                var twitterDic = accessWebSiteAsync(_twitterUrl,(doc)=>
                {
                    var result = new Dictionary<string, string>();
                    var aLinkTweets = doc.DocumentNode.Descendants("a").Single(q => q.GetAttributeValue("data-nav", string.Empty) == "tweets");
                    var tweets = aLinkTweets.Descendants("span").Single(q=>q.GetAttributeValue("class",string.Empty)== "ProfileNav-value").InnerText;
                    result.Add("twi_tweets",tweets.ToString());

                    var aLinkFollow = doc.DocumentNode.Descendants("a").Single(q => q.GetAttributeValue("data-nav", string.Empty) == "following");
                    var follow = aLinkFollow.Descendants("span").Single(q => q.GetAttributeValue("class", string.Empty) == "ProfileNav-value").InnerText;
                    result.Add("twi_following", follow.ToString());

                    var aLinkFollower = doc.DocumentNode.Descendants("a").Single(q => q.GetAttributeValue("data-nav", string.Empty) == "followers");
                    var follower = aLinkFollower.Descendants("span").Single(q => q.GetAttributeValue("class", string.Empty) == "ProfileNav-value").InnerText;
                    result.Add("twi_follower", follower.ToString());

                    return result;
                }).Result;


                /*
                var facebookDic = accessWebSiteAsync(_facebookUrl, (doc) =>
                {
                    var result = new Dictionary<string, string>();
                    var divLikes = doc.DocumentNode.Descendants("div");
                    var match = Regex.Match(doc.DocumentNode.InnerHtml,"\"likes\":[0-9]+");
                    var likes = match.Value.Replace("\"likes\":","");
                    result.Add("fbpage_likes", likes);

                    return result;
                }).Result;
                */
                
                var storageAccount = new CloudStorageAccount(new StorageCredentials(_storageAccountName,_storageAccessKey),false);
                
                var tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference(_storageTableName);

                var allData = twitterDic;
                foreach(var data in allData)
                {
                    var entry = new SocialTableEntry(data.Key, DateTime.Now.ToString("yyyy-MM-dd"))
                    {
                        Count = data.Value
                    };
                    
                    var insertOperation = TableOperation.InsertOrReplace(entry);
                    table.Execute(insertOperation);
                }
                Console.WriteLine("Complete [{0}]"+DateTime.Now.ToString());

            }
            catch(Exception e)
            {
                Console.WriteLine("Exception threw [{0}][{1}][{2}]",e.Message,e.InnerException.Message,e.StackTrace);
            }
        }

        private static async Task<Dictionary<string,string>> accessWebSiteAsync(string url,Func<HtmlAgilityPack.HtmlDocument,Dictionary<string,string>> analyzeCallback)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            using (var client = new HttpClient())
            using (var stream = await client.GetStreamAsync(new Uri(url)))
            {
                doc.Load(stream, Encoding.UTF8);
            }
            return analyzeCallback(doc);
        }
    }
}
