using ItelexCommon;
using System;
using System.Collections.Generic;

namespace ItelexMsgServer.Languages
{
	class LanguageDeutsch
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.de, "de", "Deutsch", true);
			lng.Items = new Dictionary<int, string>
			{
				{ (int)LngKeys.Yes, "ja" },
				{ (int)LngKeys.No, "nein" },
				{ (int)LngKeys.Ok, "ok" },

				{ (int)LngKeys.ServiceName, "i-telex mail/fax-service (de)" },
				{ (int)LngKeys.ConfirmOwnNumber,
				//   12345678901234567890123456789012345678901234567890123456789012345678
					"eigene i-telexnummer mit 'zl' (neue zeile) bestaetigen oder\r\nkorrekte nummer eingeben" },
				{ (int)LngKeys.EnterValidNumber, "gueltige i-telexnummer eingeben." },

				{ (int)LngKeys.EnterLoginPin, "deine pin" },
				{ (int)LngKeys.EnterOldPin, "alte pin" },
				{ (int)LngKeys.EnterNewPin, "neue pin" },
				{ (int)LngKeys.EnterNewPinAgain, "neue pin wiederholen" },
				{ (int)LngKeys.WrongPin, "falsche pin." },
				{ (int)LngKeys.InvalidPin, "ungueltige pin." },
				{ (int)LngKeys.PinsNotEqual, "pins nicht identisch." },
				{ (int)LngKeys.PinChanged, "pin geaendert." },
				{ (int)LngKeys.PinNotChanged, "pin nicht geaendert." },
				{ (int)LngKeys.SendNewLoginPin, "neue pin zusenden? (j/n)" },

				{ (int)LngKeys.NotRegistered, "nummer @1 ist noch nicht registriert.\r\nneues konto einrichten (j/n)" },
				{ (int)LngKeys.Deactivated, "das konto ist deaktiviert. bitte den betreiber des dienstes\r\nkontaktieren." },
				{ (int)LngKeys.NewAccountCreated, "ein mail/fax-service konto wurde eingerichtet." },
				{ (int)LngKeys.NewAccountActivated, "dein konto ist jetzt aktiviert." },
				//                                      12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.NewAccountTimezoneInfo, "fuer die korrekte anzeige des nachrichtendatums wird die zeitzone\r\n" +
				//   12345678901234567890123456789012345678901234567890123456789012345678
					"benoetigt. keine automatische sommerzeitumstellung."},
				{ (int)LngKeys.NewAccountEnterTimezone, "deine zeitzone" },
				{ (int)LngKeys.SendNewPinMsg, "eine neue pin wird jetzt an die nummer @1 gesendet.\r\n" +
					"bitte erneut anwaehlen und mit der neuen pin anmelden um das konto\r\n" +
					"zu aktivieren." },
				{ (int)LngKeys.CmdPrompt, "befehl" },
				{ (int)LngKeys.InvalidCommand, "ungueltiger befehl ('hilfe')." },
				{ (int)LngKeys.CommandNotYetSupported, "befehl noch nicht unterstuetzt." },
				{ (int)LngKeys.PendingMailsCleared, "@1 wartende mails geloescht." },
				{ (int)LngKeys.SendingTimeFromTo, "versandzeit @1 bis @2 uhr." },
				{ (int)LngKeys.PauseActive, "pausieren aktiv." },
				{ (int)LngKeys.PauseInactive, "pausieren inaktiv." },
				{ (int)LngKeys.AllowedSender, "erlaubter absender ist '@1'." },
				{ (int)LngKeys.AllowedSenderOff, "erlaubter absender ist aus." },
				{ (int)LngKeys.EventCode, "event-code ist '@1'." },
				{ (int)LngKeys.EventCodeOff, "event-code ist aus." },
				{ (int)LngKeys.ShowSenderActive, "zeige sender aktiv." },
				{ (int)LngKeys.ShowSenderInactive, "zeige sender inaktiv." },
				//   12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.InvalidTimezone, "ungueltige zeitzone (-12 bis 14)." },
				{ (int)LngKeys.NoAutomaticDst, "keine automatische sommerzeitumschaltung." },
				{ (int)LngKeys.ActualTimezone, "zeitzone ist jetzt @1." },
				{ (int)LngKeys.MailOrFaxReceiver, "empfaenger" },
				{ (int)LngKeys.MailSubject, "betreff" },
				{ (int)LngKeys.MailTime, "zeit" },
				{ (int)LngKeys.MailFrom, "von" },
				{ (int)LngKeys.MailTo, "an" },
				{ (int)LngKeys.MailHeader, "Dies ist eine Nachricht von i-Telex-Nummer @1." },

				{ (int)LngKeys.InputMessage, "nachricht senden (ende mit +++)" },
				{ (int)LngKeys.MailSendError, "fehler beim versenden der nachricht." },
				{ (int)LngKeys.MailSendSuccessfully, "mail versendet." },
				{ (int)LngKeys.InvalidMailAdress, "ungueltige e-mail-adresse." },
				{ (int)LngKeys.InvalidEventCode, "ungueltiger event-code (1000-9999)." },
				{ (int)LngKeys.FaxWillBeSend, "das fax wird an @1 verschickt." },
				{ (int)LngKeys.InvalidFaxNumber, "ungueltige fax-nummer." },
				{ (int)LngKeys.ForbiddenFaxNumber, "auslands- oder servicenummern nicht erlaubt." },

				{ (int)LngKeys.PunchTapeSend, "lochstreifen senden (ende mit 3 x klingel)" },
				{ (int)LngKeys.PunchTapeFilename, "name fuer die ls-datei" },
				{ (int)LngKeys.Filename, "dateiname" },
				{ (int)LngKeys.PunchTapeMailHeader, "Im Anhang die LS-Datei @1 von i-Telex-Nummer @2." },
				{ (int)LngKeys.InvalidFilename, "ungueltiger dateiname." },
				{ (int)LngKeys.InvalidData, "ungueltige oder unvollstaendige daten empfangen." },

				{ (int)LngKeys.SettingNumber, "nummer: @1" },
				{ (int)LngKeys.SettingPending, "wart. nachr.: @1" },
				{ (int)LngKeys.SettingHours, "stunden: @1 bis @2 uhr" },
				{ (int)LngKeys.SettingPaused, "pausiert: @1" },
				{ (int)LngKeys.SettingTimezone, "zeitzone: @1" },
				{ (int)LngKeys.SettingAllowedSender, "erlaubter absender: @1" },
				{ (int)LngKeys.SettingAllowRecvMails, "erlaubte mail-empfang: @1" },
				{ (int)LngKeys.SettingAllowRecvTelegram, "erlaubte telegram-empfang: @1" },
				{ (int)LngKeys.SettingMaxMailsPerDay, "max. mails pro tag: @1 (@2)" },
				{ (int)LngKeys.SettingMaxLinesPerDay, "max. zeilen pro tag: @1 (@2)" },
				{ (int)LngKeys.SettingMaxPendMails, "max. wart. mails: @1 (@2)" },

				{ (int)LngKeys.SettingTelegramChatId, "telegram chatid: @1" },
				{ (int)LngKeys.SettingTelegramChatIdLinked, "telegram chatid verbunden." },
				{ (int)LngKeys.TelegramChatIdInvalid, "telegram chatid ungueltig." },
				{ (int)LngKeys.TelegramChatIdCanNotBeLinked, "verbinden der telegram chatid fehlgeschlagen." },

				{ (int)LngKeys.SettingAssociatedMailAddr, "fest zugeordnet e-mail adresse: @1" },
				{ (int)LngKeys.SettingShowSenderAddr, "zeige absender-adresse: @1" },
				{ (int)LngKeys.SettingEventPin, "event-pin: @1" },

				{ (int)LngKeys.SendRegistrationPinText, "neue mail/fax-service pin zu deiner nummer @1 ist: @2" },
				{ (int)LngKeys.SendRedirectionPinText, "bestaetigungs-pin fuer umleitung von @1 zu @2 ist: @3\r\n" +
						"beim naechsten anruf beim mail/fax-service diese pin eingeben, um die\r\numleitung zu aktivieren."},
				//                                  12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.SendChangedPinText, "die mail/fax-service pin zu deiner nummer @1 wurde geaendert.\r\nneue pin: @2\r\n" },

				{ (int)LngKeys.PruefTextNotFound, "prueftext '@1' nicht gefunden." },

				{ (int)LngKeys.ConnectionTerminated, "die verbindung wird beendet." },
				{ (int)LngKeys.ShutDown, "der service wird zu wartungszwecken heruntergefahren." },
				{ (int)LngKeys.Aborted, "abgebrochen" },
				{ (int)LngKeys.InternalError, "interner fehler." },
			};
			return lng;
		}
	}
}
