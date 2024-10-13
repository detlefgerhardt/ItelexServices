using ItelexCommon;
using System.Collections.Generic;

namespace ItelexRundsender.Languages
{
	class LanguageDeutsch
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.de, "de", "Deutsch", true);
			lng.Items = new Dictionary<int, string>
			{
				{ (int)LngKeys.DateTimeFormat, "dd.MM.yy  HH:mm" },

				{ (int)LngKeys.ServiceName, "detlefs rundsendedienst" },
				{ (int)LngKeys.NoValidAnswerback, "kein kennungsgeber mit gueltiger anschlussnummer gefunden." },
				{ (int)LngKeys.EnterValidNumber, "gueltige nummer eingeben:" },
				{ (int)LngKeys.ConfirmOwnNumber, 
					"eigene nummer mit 'zl' (neue zeile) bestaetigen oder korrekte\r\nnummer eingeben" },
				{ (int)LngKeys.ChooseSendMode, "direkt oder zeitversetzt (d/z/g=gruppeninfo/?=hilfe) ?" },
				{ (int)LngKeys.GroupInfo, "gruppeninfo" },
				{ (int)LngKeys.GroupName, "gruppenname" },
				{ (int)LngKeys.EnterDestNumbers, "zielnummern (ende mit +)" },
				{ (int)LngKeys.NoDestNumbers, "kein zielnummern angegeben." },
				{ (int)LngKeys.EstablishConnections, "verbindungsaufbau, mom..." },
				{ (int)LngKeys.DestNumbers, "zielnummern:" },
				{ (int)LngKeys.IncludeReceiverList, "mit empfaengerliste" },
				{ (int)LngKeys.IsOk, "ok (j/n) ?" },
				{ (int)LngKeys.CreateTransmissionReport, "sendebericht wird erstellt, mom..." },
				{ (int)LngKeys.ReportOk, "ok" },
				{ (int)LngKeys.ReportError, "fehler" },
				{ (int)LngKeys.NoHelpAvailable, "keine hilfe verfuegbar" },
				{ (int)LngKeys.SomeDestNumbersNotAvailable, "einige zielnummern wurden nicht erreicht." },
				{ (int)LngKeys.SendThemDeferred, "diese zeitversetzt senden (j/n) ?" },
				{ (int)LngKeys.ReceiverPrefix, "empf:" },
				{ (int)LngKeys.PleaseSendMessage, "bitte nachricht senden (ende mit +++):" },
				{ (int)LngKeys.TerminateConnectionAndTransmit, "die verbindung wird jetzt beendet und die nachricht versendet." },
				{ (int)LngKeys.PleaseSendNumbersAndMessage, "zielnummern (ende mit +) und nachricht (ende mit +++):" },
				{ (int)LngKeys.WaitMoment, "mom..." },
				{ (int)LngKeys.TransmissionReportIntermediate, "zwischenbericht:" },
				{ (int)LngKeys.TransmissionReportIntermediateHint,
					"besetzte teilnehmer werden weiterhin angewaehlt.\r\nabschliessender sendebericht in max. 60 min." },
				{ (int)LngKeys.TransmissionReportFinal, "abschliessender sendebericht:" },

				{ (int)LngKeys.MsgTooLong, "--- nachricht zu lang ---" },

				{ (int)LngKeys.EnterLoginPin, "deine pin" },
				{ (int)LngKeys.WrongPin, "falsche pin." },
				{ (int)LngKeys.EnterOldPin, "alte pin" },
				{ (int)LngKeys.EnterNewPin, "neue pin" },
				{ (int)LngKeys.EnterNewPinAgain, "neue pin wiederholen" },
				{ (int)LngKeys.InvalidPin, "ungueltige pin." },
				{ (int)LngKeys.PinsNotEqual, "pins nicht identisch." },
				{ (int)LngKeys.PinChanged, "pin geaendert." },
				{ (int)LngKeys.PinNotChanged, "pin nicht geaendert." },
				{ (int)LngKeys.SendNewPinMsg, "eine neue pin wird jetzt an die nummer @1 gesendet.\r\n" +
					"bitte erneut anwaehlen und mit der neuen pin anmelden um das konto\r\n" +
					"zu aktivieren." },
				//                                  12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.SendChangedPinText, "deine pin zur rundsendegruppenverwaltung zu deiner nummer @1\r\nwurde geaendert. nneue pin: @2\r\n" },

				{ (int)LngKeys.NotRegistered, "nummer @1 ist noch nicht registriert.\r\nneues konto einrichten (j/n)" },
				{ (int)LngKeys.Deactivated, "das konto ist deaktiviert. bitte den betreiber dieses dienstes\r\nkontaktieren." },
				{ (int)LngKeys.SendNewLoginPin, "neue pin zusenden? (j/n)" },
				{ (int)LngKeys.NewAccountCreated, "ein rundsende-konto wurde eingerichtet." },
				//                                       12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.SendRegistrationPinText, "neue pin zur rundsendegruppenverwaltung zu deiner\r\nnummer @1 ist: @2" },

				{ (int)LngKeys.ConnectionTerminated, "die verbindung wird beendet." },
				{ (int)LngKeys.InternalError, "interner fehler." },
				{ (int)LngKeys.NewAccountActivated, "dein konto ist jetzt aktiviert." },
				{ (int)LngKeys.CmdPrompt, "befehl" },
				{ (int)LngKeys.InvalidCommand, "ungueltiger befehl ('hilfe')." },

				{ (int)LngKeys.NoGroupsFound, "keine gruppen gefunden." },
				{ (int)LngKeys.GroupNotFound, "gruppe '@1' nicht gefunden." },
				//                                       12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.InvalidCharsInGroupName, "der gruppenname darf nur buchstaben, ziffern und '-' enthalten." },
				{ (int)LngKeys.GroupSelected, "gruppe '@1' ausgewaehlt." },
				{ (int)LngKeys.GroupExists, "gruppe '@1' existiert bereits." },
				{ (int)LngKeys.GroupCreatedAndSelected, "gruppe '@1' angelegt und ausgewaehlt." },
				{ (int)LngKeys.NoGroupSelected, "keine gruppe ausgewaehlt." },
				{ (int)LngKeys.GroupDataChanged, "daten geaendert." },
				{ (int)LngKeys.GroupDeleteNotAllowed, "du darfst diese gruppe nicht loeschen." },
				{ (int)LngKeys.DeleteGroupWithMembers, "gruppe '@1' mit @2 nummern loeschen" },
				{ (int)LngKeys.GroupDeleted, "gruppe '@1' geloescht." },
				{ (int)LngKeys.NoNumbersInGroup, "keine nummern in gruppe '@1'." },
				{ (int)LngKeys.GroupHeader, "gruppe '@1'" },
				{ (int)LngKeys.InvalidNumber, "nummer @1 ist ungueltig." },
				{ (int)LngKeys.NumberAdded, "nummer @1 zugefuegt." },
				{ (int)LngKeys.DeleteNumberFromGroup, "nummer @1 aus gruppe '@2' loeschen" },
				{ (int)LngKeys.NumberNotInGroup, "nummer @1 in der aktuellen Gruppe nicht vorhanden." },
				{ (int)LngKeys.NumberDeleted, "nummer @1 geloescht." },
				//{ (int)LngKeys.GroupName, "name" },
				{ (int)LngKeys.Pin, "pin" },
			};
			return lng;
		}
	}
}
