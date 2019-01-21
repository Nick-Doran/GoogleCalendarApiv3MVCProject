using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GCalv3Test.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GCalv3Test;

namespace GCalv3Test.Services
{

    public class GoogleCalendarManager
    {
        private static string calID = "m.waqasiqbal@gmail.com"; //System.Configuration.ConfigurationManager.AppSettings["GoogleCalendarID"].ToString()
        private static string UserId = "m.waqasiqbal"; //System.Web.HttpContext.Current.User.Identity.Name
        private static string gFolder = HttpContext.Current.Server.MapPath("/App_Data/MyGoogleStorage");

        public static CalendarService GetCalendarService(GoogleTokenModel GoogleTokenModelObj)
        {
            CalendarService service = null;

            IAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = GetClientConfiguration().Secrets,
                    DataStore = new FileDataStore(gFolder),
                    Scopes = new[] { CalendarService.Scope.Calendar }
                });

            var uri = /*"http://localhost:19594/GoogleCalendarRegistration.aspx";*/System.Web.HttpContext.Current.Request.Url.ToString();
            var code = System.Web.HttpContext.Current.Request["code"];
            if (code != null)
            {
                var token = flow.ExchangeCodeForTokenAsync(UserId, code,
                    uri.Substring(0, uri.IndexOf("?")), CancellationToken.None).Result;

                // Extract the right state.
                var oauthState = AuthWebUtility.ExtracRedirectFromState(
                    flow.DataStore, UserId, HttpContext.Current.Request["state"]).Result;
                System.Web.HttpContext.Current.Response.Redirect(oauthState);
            }
            else
            {
                var result = new AuthorizationCodeWebApp(flow, uri, uri).AuthorizeAsync(UserId, CancellationToken.None).Result;
                if (result.RedirectUri != null)
                {
                    // Redirect the user to the authorization server.
                    System.Web.HttpContext.Current.Response.Redirect(result.RedirectUri);
                    //var page = System.Web.HttpContext.Current.CurrentHandler as Page;
                    //page.ClientScript.RegisterClientScriptBlock(page.GetType(),
                    //    "RedirectToGoogleScript", "window.top.location = '" + result.RedirectUri + "'", true);
                }
                else
                {
                    // The data store contains the user credential, so the user has been already authenticated.
                    service = new CalendarService(new BaseClientService.Initializer
                    {
                        ApplicationName = "My ASP.NET Google Calendar App",
                        HttpClientInitializer = result.Credential
                    });
                }
            }

            return service;
        }

        public static GoogleClientSecrets GetClientConfiguration()
        {
            using (var stream = new FileStream(gFolder + @"\client_secret.json", FileMode.Open, FileAccess.Read))
            {
                return GoogleClientSecrets.Load(stream);
            }
        }

        public static bool AddUpdateDeleteEvent(List<GoogleCalendarAppointmentModel> GoogleCalendarAppointmentModelList, List<GoogleTokenModel> GoogleTokenModelList, double TimeOffset)
        {
            //Get the calendar service for a user to add/update/delete events
            CalendarService calService = GetCalendarService(GoogleTokenModelList[0]);

            if (GoogleCalendarAppointmentModelList != null && GoogleCalendarAppointmentModelList.Count > 0)
            {
                foreach (GoogleCalendarAppointmentModel GoogleCalendarAppointmentModelObj in GoogleCalendarAppointmentModelList)
                {
                    EventsResource er = new EventsResource(calService);
                    string ExpKey = "EventID";
                    string ExpVal = GoogleCalendarAppointmentModelObj.EventID;

                    var queryEvent = er.List(calID);
                    queryEvent.SharedExtendedProperty = ExpKey + "=" + ExpVal; //"EventID=9999"
                    var EventsList = queryEvent.Execute();

                    //to restrict the appointment for specific staff only
                    //Delete this appointment from google calendar
                    if (GoogleCalendarAppointmentModelObj.DeleteAppointment == true)
                    {
                        string FoundEventID = String.Empty;
                        foreach (Event evItem in EventsList.Items)
                        {
                            FoundEventID = evItem.Id;
                            if (!String.IsNullOrEmpty(FoundEventID))
                            {
                                er.Delete(calID, FoundEventID).Execute();
                            }
                        }
                        return true;
                    }
                    //Add if not found OR update if appointment already present on google calendar
                    else
                    {
                        Event eventEntry = new Event();

                        EventDateTime StartDate = new EventDateTime();
                        EventDateTime EndDate = new EventDateTime();
                        StartDate.Date = GoogleCalendarAppointmentModelObj.EventStartTime.ToString("yyyy-MM-dd"); //"2014-11-17";
                        EndDate.Date = StartDate.Date; //GoogleCalendarAppointmentModelObj.EventEndTime

                        //Always append Extended Property whether creating or updating event
                        Event.ExtendedPropertiesData exp = new Event.ExtendedPropertiesData();
                        exp.Shared = new Dictionary<string, string>();
                        exp.Shared.Add(ExpKey, ExpVal);

                        eventEntry.Summary = GoogleCalendarAppointmentModelObj.EventTitle;
                        eventEntry.Start = StartDate;
                        eventEntry.End = EndDate;
                        eventEntry.Location = GoogleCalendarAppointmentModelObj.EventLocation;
                        eventEntry.Description = GoogleCalendarAppointmentModelObj.EventDetails;
                        eventEntry.ExtendedProperties = exp;

                        string FoundEventID = String.Empty;
                        foreach (var evItem in EventsList.Items)
                        {
                            FoundEventID = evItem.Id;
                            if (!String.IsNullOrEmpty(FoundEventID))
                            {
                                //Update the event
                                er.Update(eventEntry, calID, FoundEventID).Execute();
                            }
                        }

                        if (String.IsNullOrEmpty(FoundEventID))
                        {
                            //create the event
                            er.Insert(eventEntry, calID).Execute();
                        }

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
