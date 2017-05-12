﻿using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Classes;
using EllApp_server.Classes;
using EllApp_server.definitions;
using Newtonsoft.Json;

namespace EllApp_server.Network.Handlers
{
	public class MessageHandler
	{
		[System.Diagnostics.Conditional("DEBUG")]
		public void HandleMessage(UserContext aContext, dynamic obj, List<Session> sessions)
		{
			Console.WriteLine("MESSAGE PACKET FROM " + aContext.ClientAddress);
			string messagecontent = obj.Message;
			ChatType toType = obj.ToType;
			int from = obj.From;
			int to = obj.To;

			MakeLog(from, to, toType, messagecontent);

			switch (toType)
			{
				case ChatType.CHAT_TYPE_GLOBAL_CHAT: //Send message to all connected clients (that we have stored in sessions)
					HandleGlobalChat(sessions, from, to, toType, messagecontent);
					break;
				case ChatType.CHAT_TYPE_USER_TO_USER:
					HandleUserChat(sessions, obj);
					break;
				case ChatType.CHAT_TYPE_GROUP_CHAT:
					HandleGroupChat();
					break;
				case ChatType.CHAT_TYPE_NULL:
					HandleChatNull();
					break;
			}
		}

		private void MakeLog(int from, int to, ChatType toType, string messagecontent)
		{
			new Log_Manager
					  {
						  ChatID = Misc.CreateChatRoomID(from, to),
						  content = messagecontent,
						  to_type = toType,
						  from = from,
						  to = to
					  }.SaveLog();
		}

		private void HandleGlobalChat(List<Session> sessions, int from, int to, ChatType toType, string messagecontent)
		{
			var stCLog = new Log_Manager();
			var o = 1;
			foreach (var session in sessions)
			{
				if (session.GetUser().GetID() != from) //Do not send message to ourselves
				{
					Chat c = new Chat(ChatType.CHAT_TYPE_GLOBAL_CHAT, Misc.CreateChatRoomID(from, session.GetUser().GetID()), messagecontent, Misc.GetUsernameByID(from), Misc.GetUsernameByID(session.GetUser().GetID()));
					session.SendMessage(new MessagePacket(MessageType.MSG_TYPE_CHAT, from, session.GetUser().GetID(), JsonConvert.SerializeObject(c)));
					stCLog.content = messagecontent;
					stCLog.to_type = toType;
					stCLog.from = from;
					stCLog.to = session.GetUser().GetID();
					stCLog.SaveLog();
					o++;
				}
			}
			Console.WriteLine("Message sent to {0} users", (o - 1));
		}

		private void HandleUserChat(List<Session> sessions, dynamic obj)
		{
			Console.WriteLine("Received MSG_TYPE_CHAT_WITH_USER packet.");
			//If the receiving User is online, we can send the message to him, otherwise he will load everything at next login
			if (sessions.Any(s => s.GetUser().GetID() == (int)obj.To))
			{
				Session singleOrDefault = sessions.SingleOrDefault(s => s.GetUser().GetID() == (int)obj.To);
				if (singleOrDefault != null && singleOrDefault.GetUser().IsOnline())
				{
					Chat c = new Chat(ChatType.CHAT_TYPE_USER_TO_USER, Misc.CreateChatRoomID(obj.To, obj.From), obj.Message, obj.From, obj.To);
					Session session = sessions.SingleOrDefault(s => s.GetUser().GetID() == (int)obj.To);
					session?.SendMessage(new MessagePacket(MessageType.MSG_TYPE_CHAT, obj.From, obj.To, JsonConvert.SerializeObject(c)));
				}
			}
			else
				Console.WriteLine("DEBUG: The receiver was not online, message will be read at next login");
		}

		private void HandleGroupChat()
		{
			Console.WriteLine("MSG_TYPE_CHAT_WITH_GROUP NOT YET IMPLEMENTED");
		}

		private void HandleChatNull()
		{
			Console.WriteLine("Message Type is NULL.");
		}
	}
}
