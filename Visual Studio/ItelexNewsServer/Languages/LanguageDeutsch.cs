using ItelexCommon;
using ItelexNewsServer.Data;
using System;
using System.Collections.Generic;

namespace ItelexNewsServer.Languages
{
	class LanguageDeutsch
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.de, "de", "Deutsch", true);
			lng.Items = new Dictionary<int, string>
			{
				{ (int)LngKeys.Y, "j" },
				{ (int)LngKeys.N, "n" },
				{ (int)LngKeys.Yes, "ja" },
				{ (int)LngKeys.No, "nein" },
				{ (int)LngKeys.Ok, "ok" },

				{ (int)LngKeys.ServiceName, "i-telex news-service (de)" },
				{ (int)LngKeys.ConfirmOwnNumber,
					"eigene i-telexnummer mit 'zl' (neue zeile) bestaetigen oder\r\nkorrekte nummer eingeben" },
				{ (int)LngKeys.EnterValidNumber, "gueltige i-telexnummer eingeben." },

				{ (int)LngKeys.EnterLoginPin, "deine pin" },
				{ (int)LngKeys.WrongPin, "falsche pin." },
				{ (int)LngKeys.EnterOldPin, "alte pin" },
				{ (int)LngKeys.EnterNewPin, "neue pin" },
				{ (int)LngKeys.EnterNewPinAgain, "neue pin wiederholen" },
				{ (int)LngKeys.InvalidNewPin, "ungueltige neue pin." },
				{ (int)LngKeys.PinsNotEqual, "pins nicht identisch." },
				{ (int)LngKeys.PinChanged, "pin geaendert." },
				{ (int)LngKeys.PinNotChanged, "pin nicht geaendert." },
				{ (int)LngKeys.SendNewLoginPin, "neue pin zusenden? (j/n)" },

				{ (int)LngKeys.NotRegistered, "nummer @1 ist noch nicht registriert.\r\nneues konto einrichten (j/n)" },
				//                           12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.Deactivated, "das konto ist deaktiviert. bitte den betreiber des dienstes\r\nkontaktieren." },
				{ (int)LngKeys.NewAccountCreated, "ein news-service konto wurde eingerichtet." },
				{ (int)LngKeys.NewAccountActivated, "dein konto ist jetzt aktiviert." },
				//                                      12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.NewAccountTimezoneInfo, "fuer die korrekte anzeige des nachrichtendatums wird die zeitzone\r\n" +
				//   12345678901234567890123456789012345678901234567890123456789012345678
					"benoetigt. keine automatische sommerzeitumstellung."},
				{ (int)LngKeys.NewAccountEnterTimezone, "deine zeitzone" },
				{ (int)LngKeys.SendNewPinMsg, "eine neue pin wird jetzt an die nummer @1 gesendet.\r\n" +
					"bitte erneut anwaehlen und mit der neuen pin anmelden." },
				{ (int)LngKeys.EnterRedirectConfirmPin, "bestaetigungs-pin fuer umleitung" },
				{ (int)LngKeys.RedirectActivated, "umleitung zu @1 ist jetzt aktiv." },
				{ (int)LngKeys.RedirectAlreadyActive, "umleitung zu @1 ist bereits aktiv." },

				{ (int)LngKeys.CmdPrompt, "befehl" },
				{ (int)LngKeys.InvalidCommand, "ungueltiger befehl ('hilfe')." },
				{ (int)LngKeys.CommandNotYetSupported, "befehl noch nicht unterstuetzt." },
				{ (int)LngKeys.SubscribeChannel, "kanal @1 '@2' abonniert." },
				{ (int)LngKeys.UnsubscribeChannel, "kanal @1 '@2' abbestellt." },
				{ (int)LngKeys.ChannelNotFound, "kanal '@1' nicht gefunden." },
				{ (int)LngKeys.PendingMsgsCleared, "@1 wartende nachrichten geloescht." },
				{ (int)LngKeys.SendingTimeFromTo, "versandzeit @1 bis @2 uhr." },
				{ (int)LngKeys.PauseActive, "pausieren aktiv." },
				{ (int)LngKeys.PauseInactive, "pausieren inaktiv." },
				{ (int)LngKeys.InvalidRedirectNumber, "ungueltige i-telexnummer." },
				{ (int)LngKeys.RedirectInactive, "umleitung inaktiv." },
				{ (int)LngKeys.SendRedirectConfirmPin, "bestaetigungs-pin wird an @1 gesendet.\r\n" +
				//   12345678901234567890123456789012345678901234567890123456789012345678
					"falls du von dieser nummer anrufst, dann die verbindung jetzt\r\nbeenden." },
				{ (int)LngKeys.RedirectNotConfirmed, "umleitung wurde nicht bestaetigt und geloescht." },
				{ (int)LngKeys.InvalidTimezone, "ungueltige zeitzone (-12 bis 14)." },
				{ (int)LngKeys.NoAutomaticDst, "keine automatische sommerzeitumschaltung." },
				{ (int)LngKeys.ActualTimezone, "zeitzone ist jetzt @1." },
				{ (int)LngKeys.ActualMsgFormat, "nachrichtenformat ist jetzt @1." },
				{ (int)LngKeys.MsgFormatStandard, "standard" },
				{ (int)LngKeys.MsgFormatShort, "kurz" },
				{ (int)LngKeys.NoMatchingChannels, "keine passenden kanaele." },

				{ (int)LngKeys.SettingNumber, "nummer: @1" },
				{ (int)LngKeys.SettingPending, "wart. nachr.: @1" },
				{ (int)LngKeys.SettingHours, "stunden: @1 bis @2 uhr" },
				{ (int)LngKeys.SettingRedirect, "umleitung: @1" },
				{ (int)LngKeys.SettingPaused, "pausiert: @1" },
				{ (int)LngKeys.SettingTimezone, "zeitzone: @1" },
				{ (int)LngKeys.SettingMsgFormat, "nachr. format: @1" },
				{ (int)LngKeys.SettingMaxPendMsgs, "max. wart. nachr.: @1" },

				{ (int)LngKeys.SendRegistrationPinText, "neue news-service pin zu deiner nummer @1 ist: @2" },
				{ (int)LngKeys.SendRedirectionPinText, "bestaetigungs-pin fuer umleitung von @1 zu @2\r\nist: @3\r\n" +
						"beim naechsten anruf beim news-service diese pin eingeben, um die\r\numleitung zu aktivieren."},
				{ (int)LngKeys.SendChangedPinText, "die news-service pin zu deiner nummer @1 wurde geaendert.\r\nneue pin: @2\r\n" },
				{ (int)LngKeys.SendChangeNotificationText, "aenderungen deines news-service abos" },

				{ (int)LngKeys.ConnectionTerminated, "die verbindung wird beendet." },
				{ (int)LngKeys.ShutDown, "der service wird zu wartungszwecken heruntergefahren." },
				{ (int)LngKeys.Aborted, "abgebrochen" },
				{ (int)LngKeys.InternalError, "interner fehler." },

				{ (int)LngKeys.LocalChannels, "lokale kanaele" },
				{ (int)LngKeys.NoLocalChannels, "keine lokalen kanaele gefunden." },
				{ (int)LngKeys.LocalChannelName, "kanalname" },
				{ (int)LngKeys.LocalChannelNo, "lokale kanalnummer" },
				{ (int)LngKeys.IsChannelPublic, "oeffentlicher kanal (j/n)" },
				{ (int)LngKeys.ChannelLanguage, "sprache" },
				{ (int)LngKeys.InvalidLanguage, "ungueltige sprache '@1'" },
				{ (int)LngKeys.Pin, "kanal-pin" },
				{ (int)LngKeys.InvalidChannelPin, "ungueltige pin '@1'." },
				{ (int)LngKeys.InvalidChannelNo, "ungueltige kanal-nummer '@1'." },
				{ (int)LngKeys.ChannelNameExists, "der kanal '@1' existiert bereits." },
				{ (int)LngKeys.ChannelCreatedAndSelected, "kanal @1 angelegt und ausgewaehlt." },
				{ (int)LngKeys.NoChannelSelected, "kein kanal ausgewaehlt." },
				{ (int)LngKeys.ChannelDataChanged, "kanal-daten geaendert." },
				{ (int)LngKeys.ChannelSelected, "kanal @1 ausgewaehlt." },
				{ (int)LngKeys.ChannelDeleteNotAllowed, "du darfst diesen kanal nicht loeschen." },
				{ (int)LngKeys.DeleteChannelWithSubscriptions, "kanal @1 mit @2 abos loeschen (j/n)" },
				{ (int)LngKeys.ChannelDeleted, "kanal @1 geloescht." },
				{ (int)LngKeys.NoNumbersInChannel, "keine abonnenten fuer kanal @1." },
				{ (int)LngKeys.ShowNumbersNotAllowed, "@1 ist kein lokaler oeffentlicher kanal." },
				{ (int)LngKeys.ChannelHeader, "kanal @1" },
				{ (int)LngKeys.NotALocalChannel, "@1 ist kein lokaler kanal." },
				{ (int)LngKeys.MsgTitle, "titel" },
				{ (int)LngKeys.SendToChannel, "sende in kanal @1." },
				{ (int)LngKeys.SendACopy, "kopie an @1 (j/n)" },
				{ (int)LngKeys.NoSubscribersWhenSending, "dieser kanal hat keine abonnenten. trotzdem senden (j/n)" },
				//                            12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.InputMessage, "nachricht senden (ende mit +++)" },
				{ (int)LngKeys.SendMessage, "nachricht versenden (j/n)" },
				{ (int)LngKeys.MessageSent, "nachricht wird versendet." },
			};
			return lng;
		}
	}
}
