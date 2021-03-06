﻿namespace LuisBot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Builder.Luis.Models;
    using Microsoft.Bot.Connector;

    using System.Net.Http;
    using Newtonsoft.Json;
    using System.Text;

    /*[LuisModel("c9bdb42b-9e3e-4995-bd91-67b7162d65e2", "fc29b1d583574a8cbb7b80c2c2066eb4")]*/
    [LuisModel("a4c3f93b-6a1a-4542-928e-5b6d28eb2828", "fc29b1d583574a8cbb7b80c2c2066eb4")]
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {
        /*private const string EntityGeographyCity = "builtin.geography.city";

        private const string EntityHotelName = "Hotel";

        private const string EntityAirportCode = "AirportCode";

        private IList<string> titleOptions = new List<string> { "“Very stylish, great stay, great staff”", "“good hotel awful meals”", "“Need more attention to little things”", "“Lovely small hotel ideally situated to explore the area.”", "“Positive surprise”", "“Beautiful suite and resort”" };*/

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = "Disculpa, no logro entender, menciona 'ayuda' para transferirte con un asesor.";

            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("identidad")]
        public async Task Search(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            /*var message = await activity;
            await context.PostAsync($"Welcome to the Hotels finder! We are analyzing your message: '{message.Text}'...");

            var hotelsQuery = new HotelsQuery();

            EntityRecommendation cityEntityRecommendation;

            if (result.TryFindEntity(EntityGeographyCity, out cityEntityRecommendation))
            {
                cityEntityRecommendation.Type = "Destination";
            }

            var hotelsFormDialog = new FormDialog<HotelsQuery>(hotelsQuery, this.BuildHotelsForm, FormOptions.PromptInStart, result.Entities);

            context.Call(hotelsFormDialog, this.ResumeAfterHotelsFormDialog);*/

            string resLlamada;
            string texto;

            HttpClient request = new HttpClient();            
            resLlamada = await request.GetStringAsync("https://functionsura.azurewebsites.net/api/ConsultaUsuarios");
            dynamic obj = JsonConvert.DeserializeObject(resLlamada);

            //texto = String.Format("Hola {0}, el teléfono de {1} es {2}", obj[0].Nombre, obj[1].Nombre, obj[1].Telefono);
            texto = $"Hola {obj[0].Nombre}, el teléfono de {obj[1].Nombre} es {obj[1].Telefono}";

            await context.PostAsync(texto);
        }

        [LuisIntent("acepto")]
        public async Task Reviews(IDialogContext context, LuisResult result)
        {

            string resLlamada;
            string texto;

            HttpClient request = new HttpClient();
            resLlamada = await request.GetStringAsync("https://functionsura.azurewebsites.net/api/ConsultaUsuarios");
            dynamic obj = JsonConvert.DeserializeObject(resLlamada);

            //texto = String.Format("Hola {0}, el teléfono de {1} es {2}", obj[0].Nombre, obj[1].Nombre, obj[1].Telefono);
            texto = $"Hola {obj[0].Nombre}, que buena decisión";

            await context.PostAsync(texto);



        }

        [LuisIntent("ayuda")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            HttpClient request = new HttpClient();            
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://functionsura.azurewebsites.net/api/InsertarEncuesta?code=V1aYP5x3IPGjSCTcLsx4g68jZAkIkrojIagba6JN6v89lsBSfUwxYA==");

            var obj = new { userId = "2", respuesta1 = "israel12", respuesta2 = "mal12" };
            string str = JsonConvert.SerializeObject(obj);

            requestMessage.Content = new StringContent(str, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await request.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();
            
            await context.PostAsync(responseString);
        }

        private IForm<HotelsQuery> BuildHotelsForm()
        {
            OnCompletionAsyncDelegate<HotelsQuery> processHotelsSearch = async (context, state) =>
            {
                var message = "Buscando";
                if (!string.IsNullOrEmpty(state.Destination))
                {
                    message += $" en {state.Destination}...";
                }
                else if (!string.IsNullOrEmpty(state.AirportCode))
                {
                    message += $" cerca {state.AirportCode.ToUpperInvariant()} airport...";
                }

                await context.PostAsync(message);
            };

            return new FormBuilder<HotelsQuery>()
                .Field(nameof(HotelsQuery.Destination), (state) => string.IsNullOrEmpty(state.AirportCode))
                .Field(nameof(HotelsQuery.AirportCode), (state) => string.IsNullOrEmpty(state.Destination))
                .OnCompletion(processHotelsSearch)
                .Build();
        }

        private async Task ResumeAfterHotelsFormDialog(IDialogContext context, IAwaitable<HotelsQuery> result)
        {
            try
            {
                var searchQuery = await result;

                var hotels = await this.GetHotelsAsync(searchQuery);

                await context.PostAsync($"I found {hotels.Count()} hotels:");

                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();

                foreach (var hotel in hotels)
                {
                    HeroCard heroCard = new HeroCard()
                    {
                        Title = hotel.Name,
                        Subtitle = $"{hotel.Rating} starts. {hotel.NumberOfReviews} reviews. From ${hotel.PriceStarting} per night.",
                        Images = new List<CardImage>()
                        {
                            new CardImage() { Url = hotel.Image }
                        },
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "More details",
                                Type = ActionTypes.OpenUrl,
                                Value = $"https://www.bing.com/search?q=hotels+in+" + HttpUtility.UrlEncode(hotel.Location)
                            }
                        }
                    };

                    resultMessage.Attachments.Add(heroCard.ToAttachment());
                }

                await context.PostAsync(resultMessage);
            }
            catch (FormCanceledException ex)
            {
                string reply;

                if (ex.InnerException == null)
                {
                    reply = "You have canceled the operation.";
                }
                else
                {
                    reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                }

                await context.PostAsync(reply);
            }
            finally
            {
                context.Done<object>(null);
            }
        }

        private async Task<IEnumerable<Hotel>> GetHotelsAsync(HotelsQuery searchQuery)
        {
            var hotels = new List<Hotel>();

            // Filling the hotels results manually just for demo purposes
            for (int i = 1; i <= 5; i++)
            {
                var random = new Random(i);
                Hotel hotel = new Hotel()
                {
                    Name = $"{searchQuery.Destination ?? searchQuery.AirportCode} Hotel {i}",
                    Location = searchQuery.Destination ?? searchQuery.AirportCode,
                    Rating = random.Next(1, 5),
                    NumberOfReviews = random.Next(0, 5000),
                    PriceStarting = random.Next(80, 450),
                    Image = $"https://placeholdit.imgix.net/~text?txtsize=35&txt=Hotel+{i}&w=500&h=260"
                };

                hotels.Add(hotel);
            }

            hotels.Sort((h1, h2) => h1.PriceStarting.CompareTo(h2.PriceStarting));

            return hotels;
        }
    }
}
