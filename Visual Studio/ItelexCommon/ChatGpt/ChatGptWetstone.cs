using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Whetstone.ChatGPT.Models;
using Whetstone.ChatGPT;
using System.Diagnostics;

namespace ItelexCommon.ChatGpt
{
	/// <summary>
	/// Wetstone ChatGPT Library
	/// https://github.com/johniwasz/whetstone.chatgpt
	/// </summary>
	public class ChatGptWetstone: ChatGptAbstract
	{
		private const string TAG = nameof(ChatGptWetstone);

		private string MODEL = ChatGPT4Models.GPT4;

		public ChatGptWetstone()
		{
		}

		public override async void Test()
		{
			/*
			IChatGPTClient client = new ChatGPTClient("YOURAPIKEY");
			var gptRequest = new ChatGPTCompletionRequest
			{
				Model = ChatGPT35Models.Davinci003,
				Prompt = "How is the weather?"
			};
			*/

			var gptRequest = new ChatGPTChatCompletionRequest
			{
				//Model = ChatGPT35Models.Turbo,
				Model = ChatGPT4Models.GPT4,
				Messages = new List<ChatGPTChatCompletionMessage>()
				{
					/*
					new ChatGPTChatCompletionMessage()
					{
						Role = MessageRole.System,
						Content = "You are a helpful assistant."
					},
					new ChatGPTChatCompletionMessage()
					{
						Role = MessageRole.User,
						Content = "Who won the world series in 2020?"
					},
					new ChatGPTChatCompletionMessage()
					{
						Role = MessageRole.Assistant,
						Content = "The Los Angeles Dodgers won the World Series in 2020."
					},
					*/
					new ChatGPTChatCompletionMessage()
					{
						//Role = MessageRole.User,
						//Content = "Wer war Konrad Zuse?"
						Content = "erzähle mir eine geschichte von freddy, dem rennauto."
					}
				},
				MaxTokens = 0,
				Temperature = 0.5f,
				TopP = 0.5f,
			};

			IChatGPTClient client = new ChatGPTClient(KEY);

			var response = await client.CreateChatCompletionAsync(gptRequest);

			string answer = response.GetCompletionText();
		}

		public override string GetModel()
		{
			return MODEL.ToLower();
		}

		public override async Task<string> Request(string reqStr, float? temperature = null, float? top_p = null)
		{
			if (temperature == null) temperature = 0.2f;
			if (top_p == null) top_p = 0.2f;

			ChatGPTChatCompletionRequest gptRequest = new ChatGPTChatCompletionRequest
			{
				//Model = ChatGPT35Models.Turbo,
				Model = ChatGPT4Models.GPT4,
				Messages = new List<ChatGPTChatCompletionMessage>()
				{
					/*
					new ChatGPTChatCompletionMessage()
					{
						Role = MessageRole.System,
						Content = "You are a helpful assistant."
					},
					new ChatGPTChatCompletionMessage()
					{
						Role = MessageRole.User,
						Content = "Who won the world series in 2020?"
					},
					new ChatGPTChatCompletionMessage()
					{
						Role = MessageRole.Assistant,
						Content = "The Los Angeles Dodgers won the World Series in 2020."
					},
					*/
					new ChatGPTChatCompletionMessage()
					{
						Role = ChatGPTMessageRoles.User,
						Content = reqStr
					}
				},
				Temperature = temperature.Value,
				TopP = top_p.Value,
				MaxTokens = 0
			};

			IChatGPTClient client = new ChatGPTClient(KEY);
			ChatGPTChatCompletionResponse response = await client.CreateChatCompletionAsync(gptRequest);
			string answer = response.GetCompletionText();
			return answer;
		}
	}
}
