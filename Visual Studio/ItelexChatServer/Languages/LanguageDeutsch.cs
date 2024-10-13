using ItelexCommon;
using System.Collections.Generic;

namespace ItelexChatServer.Languages
{
	class LanguageDeutsch
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.de, "de", "Deutsch", true);
			lng.Items = new Dictionary<int, string>
			{
				{ (int)LngKeys.ServiceName, "i-telex konferenzdienst" },
				//{ (int)LngKeys.InputAnswerback, "eigenen kennungsgeber mit 'zl' (neue zeile) bestaetigen" },
				//{ (int)LngKeys.NoAnswerbackReceived, "kein kennungsgeber empfangen." },
				{ (int)LngKeys.InputHelp, "bitte einen lesbaren kurznamen verwenden" },
				{ (int)LngKeys.EnterShortName, "kurzname (? fuer hilfe):" },
				{ (int)LngKeys.ShortNameInUse, "dieser kurzname wird aktuell schon verwendet." },
				{ (int)LngKeys.HelpHint, "? fuer hilfe" },
				{ (int)LngKeys.NewEntrant, "neuer teilnehmer" },
				{ (int)LngKeys.HasLeft, "hat die konferenz verlassen." },
				{ (int)LngKeys.HoldConnection, "verbindung halten:" },
				{ (int)LngKeys.On, "ein" },
				{ (int)LngKeys.Off, "aus" },
				{ (int)LngKeys.Subscribers, "teilnehmer" },
				{ (int)LngKeys.None, "keine" },
				{ (int)LngKeys.HistRestart, "neustart" },
				{ (int)LngKeys.HistLogin, "anmeldung" },
				{ (int)LngKeys.HistLogoff, "abmeldung" },
				{ (int)LngKeys.Help, 
						"? = hilfe" +
						"\r\nklingel = schreibanfrage (+? fuer ende)" +
						"\r\nwerda = konferenzliste" +
						"\r\n'=' = verlauf" +
						"\r\n'/' = verbindung halten ein/aus" +
						"\r\n'+' = benachrichtigungen konfigurieren" +
						"\r\nbeenden mit st-taste" },
				{ (int)LngKeys.ShutDown, "der konferenzdienst wird aus wartungsgruenden\r\nheruntergefahren." },
				{ (int)LngKeys.CmdHelp,
						"kommandos im befehlsmodus:" +
						"\r\nhilfe" +
						"\r\nende = befehlsmodus beenden" +
						"\r\nhalten ein/aus = verbindung halten" +
						"\r\nliste tln" +
						"\r\nrun hilfe" +
						"\r\nabmelden" },
				{ (int)LngKeys.CmdRunHelp, 
						"\r\nrun hilfe" +
						"\r\nrun hamurabi" +
						"\r\nrun biorhyhtmus" },
				{ (int)LngKeys.CmdError, "cmd fehler" },

				{ (int)LngKeys.NotificationStartMsg,
						//  123456789012345678901234567890123456789012345678901234567890
							"benachrichtungen aendern (+,-,l,?) ?" },
				//{ (int)LngKeys.NotificationAddDelShow, "(+)neue nummer, (-)nummer loeschen ?" },
				{ (int)LngKeys.NotificationEnterNumber, "i-telexnummer:" },
				{ (int)LngKeys.NotificationEnterExtension, "optionale durchwahl nummer:" },
				//{ (int)LngKeys.NotificationEnterPin, "pin-nummer:" },
				{ (int)LngKeys.NotificationOnLogin, "bei anmeldung (j,n)?" },
				{ (int)LngKeys.NotificationOnLogoff, "bei abmeldung (j,n)?" },
				{ (int)LngKeys.NotificationOnWriting, "bei schreiben (j,n)?" },
				{ (int)LngKeys.NotificationInvalidNumber, "ungueltige nummer." },
				{ (int)LngKeys.NotificationUnknownNumber, "nummer auf tln.-server nicht bekannt." },
				//{ (int)LngKeys.NotificationInvalidPin, "ungueltige pin-nummer @1." },
				{ (int)LngKeys.NotificationNone, "keine benachrichtigungen." },
				{ (int)LngKeys.NotificationAlreadyActive, "benachrichtigung fuer @1 bereits aktiv." },
				{ (int)LngKeys.NotificationNotActive, "benachrichtigung fuer @1 nicht aktiv." },
				{ (int)LngKeys.NotificationNowActive, "benachrichtigung fuer @1 ist jetzt aktiv." },
				//{ (int)LngKeys.NotificationPresentPin, "pin-nummer zum loeschen der benachrichtigung: @1." },
				{ (int)LngKeys.NotificationDeleted, "benachrichtigung fuer @1 geloescht." },
				{ (int)LngKeys.NotificationText, "benachrichtigung vom konferenzdienst @1" },
				{ (int)LngKeys.NotificationAddConfMsg,
							"deine i-telexnummer @1 wurde soeben zu\r\n" +
							"konferenzdienst-benachrichtungen hinzugefuegt.\r\n" +
							"durch benutzer: @2" },
				{ (int)LngKeys.NotificationDelConfMsg,
						//  123456789012345678901234567890123456789012345678901234567890
							"deine i-telexnummer @1 wurde soeben aus den\r\n" +
							"konferenzdienst-benachrichtungen geloescht.\t\n" +
							"durch benutzer: @2"},
				{ (int)LngKeys.NotificationAbuseMsg, "bei missbrauch bitte eine nachricht an @1." },
				{ (int)LngKeys.NotificationHelp,
						//  1234567890123456789012345678901234567890123456789012345678
							"hier kannst du eine i-telexnummer konfigurieren, an die\r\n" +
							"automatisch eine benachrichtigung gesendet wird, wenn sich\r\n" +
							"benutzer an-/abmelden oder eine nachricht schreiben. so\r\n" +
							"musst du nicht dauerhaft angemeldet sein, um mitzube-\r\n" +
							"kommen, wenn sich etwas tut.\r\n" +
							" + benachrichtigung hinzufuegen\r\n" +
							" - benachrichtigung loeschen\r\n" +
							" l liste der benachrichtigungen\r\n" +
							" ? diese hilfe\r\n" +
							"um eine bereits eingerichtete benachrichtung zu aendern,\r\n" +
							"benachrichtigung loeschen und neu anlegen." },
				{ (int)LngKeys.Notifications, "benachrichtigungen fuer @1:" },
				{ (int)LngKeys.NotificationLogin, "anmeldung" },
				{ (int)LngKeys.NotificationLogoff, "abmeldung" },
				{ (int)LngKeys.NotificationWriting, "schreiben" },

				{ (int)LngKeys.ConnectionTerminated, "die verbindung wird beendet." },
				{ (int)LngKeys.InternalError, "interner fehler." },
			};
			return lng;
		}
	}
}
