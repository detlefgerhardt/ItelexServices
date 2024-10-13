using ItelexChatServer.Languages;
using ItelexCommon;
using ItelexCommon.Connection;
using System;
using System.Diagnostics;

namespace ItelexChatServer.Actions
{
	class HamurabiAction : ActionBase
	{
		private const string TAG = nameof(HamurabiAction);

		private const int FOOD_REQUIRED_PER_PERSON = 20;

		private Random _rnd;
		//private ItelexConnection _itelex;

		private int _totalDied; // total people that starved
		private double _totalDiedPercent; // total percent if people that starved
		private int _year; // current year
		private int _people; // people
		private int _bushels; // bushels in store
		private int _acres; // acres

		private int harvest; // harvest
		private int buschelsEatenByRats; // bushels eaten by rats
		private int bushelsHarvestedPerAcres; // bushels harvested per acres
		private int newPeople; // people that come the the city
		private int plague; // plague (<=0: plague active)
		private int dead; // people that starved in the current year

		public int Score => _bushels + _acres * 20;

		public int Year => _year;

		private string debugName => _chatConnection != null ? _chatConnection.ConnectionName : "null";

		public HamurabiAction(ActionBase.ActionCallTypes actionCallType, ItelexLogger itelexLogger) : 
				base(Actions.Hamurabi, LanguageDefinition.GetLanguageById(LanguageIds.en), actionCallType, itelexLogger)
		{
		}

		public override void Run(IncomingChatConnection chatConnection, bool debug)
		{
			base.Run(chatConnection, debug);
			Start();

			if (_actionCallType == ActionCallTypes.FromCmd)
			{
				// return to command mode
				_chatConnection.StartCommandMode();
			}
		}

		private void Start()
		{
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"{debugName}");
			_totalDied = 0;
			_totalDiedPercent = 0;
			_year = 0;
			_people = 95;
			_bushels = 2800;
			harvest = 3000;
			buschelsEatenByRats = harvest - _bushels;
			bushelsHarvestedPerAcres = 3;
			_acres = harvest / bushelsHarvestedPerAcres;
			newPeople = 5;
			plague = 1;
			dead = 0;

			_rnd = new Random();

			//                     12345678901234567890123456789012345678901234567890123456789
			_chatConnection.SendAscii("\r\n                      h a m u r a b i");
			_chatConnection.SendAscii("\r\n                      ---------------");
			_chatConnection.SendAscii("\r\ntry your hand at govering ancient sumeria for a ten-year");
			_chatConnection.SendAscii("\r\nterm of office.");
			_chatConnection.SendAscii("\r\n");
			_chatConnection.SendAscii("\r\nhamurabi was first developled under the name 'king of");
			_chatConnection.SendAscii("\r\nsumeria' or 'the sumer game' by Doug Dyment in 1968 at");
			_chatConnection.SendAscii("\r\ndigital equipment corporation (dec) in the programming");
			_chatConnection.SendAscii("\r\nlanguage 'focal'. this version is based on the basic");
			_chatConnection.SendAscii("\r\nversion from the book 'basic computer games' that was");
			_chatConnection.SendAscii("\r\npublished 1978.");
			_chatConnection.SendAscii("\r\n");
			_chatConnection.SendAscii("\r\nthe rules:");
			_chatConnection.SendAscii("\r\nacre trading is from 17 to 26 bushels.");
			_chatConnection.SendAscii("\r\neach inhabitant needs 20 bushels per year to survive.");
			_chatConnection.SendAscii("\r\nto plant 10 acres 5 bushels are consumed and 1 inhabitant");
			_chatConnection.SendAscii("\r\nis needed.");

			while (true)
			{
				if (!Turn())
				{
					break;
				}
			}
			Result();
		}

		public bool Turn()
		{
			_year++;
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Turn), $"{debugName} year={_year} people={_people} acres={_acres} bushels={_bushels}");

			_chatConnection.SendAscii("\r\n");
			_chatConnection.SendAscii($"\r\ni beg to report you in year {_year}:");
			_chatConnection.SendAscii($"\r\n{dead} people starved, {newPeople} came to the city.");
			_people = _people + newPeople;
			if (plague <= 0)
			{
				_people = _people / 2;
				_chatConnection.SendAscii("\r\na horrible plague struck. half of the people died.");
			}
			_chatConnection.SendAscii($"\r\npopulation is now {_people}.");
			_chatConnection.SendAscii($"\r\nthe city owns {_acres} acres.");
			_chatConnection.SendAscii($"\r\nyou harvested {bushelsHarvestedPerAcres} bushels per acre.");
			if (buschelsEatenByRats > 0)
			{
				_chatConnection.SendAscii($"\r\nrats ate {buschelsEatenByRats} bushels.");
			}
			_chatConnection.SendAscii($"\r\nyou now have {_bushels} bushels in store.");

			// buy acres of land

			int num;

			int rndNum;
			if (!_debug)
			{
				rndNum = IntRnd(0, 9);
			}
			else
			{
				rndNum = 0;
			}
			int acresPrice = 17 + rndNum;
			_chatConnection.SendAscii("\r\n");
			_chatConnection.SendAscii($"\r\nland is trading at {acresPrice} bushels per acre.");
			while (true)
			{
				Debug.WriteLine($"acres={_acres}, bushels={_bushels}");
				_chatConnection.SendAscii("\r\nhow many acres do you wish to buy");
				num = InputNumber();
				if (num < 0)
				{
					MsgCanNotDo();
					continue;
				}
				if (acresPrice * num > _bushels)
				{
					MsgNotEnoughBushels(_bushels);
					continue;
				}
				break;
			}
			if (num != 0)
			{
				// buy acres
				_acres = _acres + num;
				_bushels -= num * acresPrice;
				//rndNum = 0;
			}
			else
			{
				while (true)
				{
					Debug.WriteLine($"acres={_acres}, bushels={_bushels}");
					_chatConnection.SendAscii("\r\nhow many acres do you wish to sell");
					num = InputNumber();
					if (num < 0)
					{
						MsgCanNotDo();
						continue;
					}
					if (num >= _acres) // why not Q > A
					{
						MsgNotEnoughAcres(_acres);
						continue;
					}
					break;
				}
				_acres -= num;
				_bushels += num * acresPrice;
				//rndNum = 0;
			}

			// feed people

			Console.WriteLine("");
			while (true)
			{
				Debug.WriteLine($"acres={_acres}, bushels={_bushels}, people={_people}");
				_chatConnection.SendAscii("\r\nhow many bushels do you wish to feed your people");
				num = InputNumber();
				if (num < 0)
				{
					MsgCanNotDo();
					continue;
				}
				if (num > _bushels)
				{
					MsgNotEnoughBushels(_bushels);
					continue;
				}
				break;
			}
			_bushels -= num;
			int feed = num;
			Debug.WriteLine($"bushels={_bushels}");
			//rndNum = 1;

			// plant acres with seed

			Console.WriteLine("");
			int plant;
			while (true)
			{
				Debug.WriteLine($"acres={_acres}, bushels={_bushels}, people={_people}");
				_chatConnection.SendAscii("\r\nhow many acres do you want to plant with seed");
				plant = InputNumber();
				if (num < 0)
				{
					MsgCanNotDo();
					continue;
				}
				if (plant > _acres)
				{
					MsgNotEnoughAcres(_acres);
					continue;
				}
				if (plant > _bushels * 2)
				{
					// not enough grain for seed, 1 buschel per 2 acre
					MsgNotEnoughBushels(_bushels);
					continue;
				}
				if (plant >= _people * 10)
				{
					// not enouth people to tend the crops, 1 people per 10 acre
					MsgNotEnoughPeople(_people);
					continue;
				}
				break;
			}
			// need 2 bushels per acres for planting
			_bushels = _bushels - plant / 2;
			Debug.WriteLine($"bushels={_bushels}");

			// a beautiful harvest
			if (!_debug)
			{
				rndNum = IntRnd(1, 5);
			}
			else
			{
				rndNum = _year % 5; if (rndNum > 5) rndNum -= 5;
			}
			bushelsHarvestedPerAcres = rndNum;
			harvest = plant * bushelsHarvestedPerAcres;

			// rats running wild?
			if (!_debug)
			{
				rndNum = IntRnd(1, 5);
			}
			else
			{
				rndNum = _year % 5; if (rndNum > 5) rndNum -= 5;
			}
			if ((rndNum & 1) == 0)
			{
				// a 40% chance that rats are running wild, rndnum=2,4
				buschelsEatenByRats = _bushels / rndNum; // eat 1/2 or 1/4 of the bushels
			}
			else
			{
				// radNum = 1,3,5
				buschelsEatenByRats = 0;
			}
			_bushels = _bushels - buschelsEatenByRats + harvest;

			// new people
			if (!_debug)
			{
				rndNum = IntRnd(1, 5);
			}
			else
			{
				rndNum = _year % 5; if (rndNum > 5) rndNum -= 5;
			}
			newPeople = rndNum * (20 * _acres + _bushels) / _people / 100 + 1;

			// how many people had full tummies, need 20 bushels per person
			int alive = feed / FOOD_REQUIRED_PER_PERSON;

			// horror: a 15% chance of plague
			if (!_debug)
			{
				plague = IntRnd(0, 99) < 15 ? 0 : 1;
			}
			else
			{
				plague = 1;
			}

			Debug.WriteLine($"people={_people}, newPeople={newPeople}, alive={alive}");
			if (alive < _people)
			{
				// some people starved
				dead = _people - alive;
				if (dead > _people * 0.45)
				{
					// more then 45% starved: impeachment
					MsgMissmanagement();
					return false;
				}
			}

			_totalDiedPercent = ((_year - 1) * _totalDiedPercent + dead * 100 / _people) / _year;
			_people = alive;
			_totalDied += dead;

			return true;
		}

		public void Result()
		{
			_chatConnection.SendAscii($"\r\n");
			_chatConnection.SendAscii($"\r\nin your 10-year term of office {_totalDiedPercent} percent");
			_chatConnection.SendAscii("\r\nof the population starved per year on the average, i.e. a");
			_chatConnection.SendAscii($"\r\ntotal of {_totalDied} people died!!");
			int acresPerPerson = _acres / _people;
			_chatConnection.SendAscii($"\r\nyou started with 10 acres per person and ended with {acresPerPerson}");
			_chatConnection.SendAscii("\r\nacres per person.");
			_chatConnection.SendAscii("\r\n");

			if (_totalDiedPercent > 33 || _acres < 7)
			{
				MsgMissmanagement();
				return;
			}

			if (_totalDiedPercent > 10 || _acres < 9)
			{
				_chatConnection.SendAscii("\r\nyour heavy-handed performance smacks of nero and ivan iv.");
				_chatConnection.SendAscii("\r\nthe people (remainung) find you an unpleasant ruler, and,");
				_chatConnection.SendAscii("\r\nfrankly, hate your guts");
				return;
			}

			if (_totalDiedPercent > 3 || _acres >= 10)
			{
				_chatConnection.SendAscii("\r\nyour perfomance could have been somewhat better, but really");
				_chatConnection.SendAscii($"\r\nwasn't too bad at all. {Math.Floor(_people * 0.8 * _rnd.NextDouble())} people would dearly");
				_chatConnection.SendAscii("\r\nlike to see you assassinated but we alle have our trival");
				_chatConnection.SendAscii("\r\nproblems.");
				return;
			}

			// CHARLEMANGE = Karl der Große
			// DISRAELI = Benjamin Disraeli, 1st Earl of Beaconsfield
			_chatConnection.SendAscii("\r\na fantastic performance!!! charlemange, disraeli, and");
			_chatConnection.SendAscii("\r\njefferson combined could not jave done better.");
		}

		private void MsgMissmanagement()
		{
			_chatConnection.SendAscii("\r\n");
			_chatConnection.SendAscii($"\r\nyou starved {dead} people in one year!!!");
			_chatConnection.SendAscii("\r\ndue to this extrem mismanagement you have not only been");
			_chatConnection.SendAscii("\r\nimpeached and thrown out of office but you have also been");
			_chatConnection.SendAscii("\r\ndeclared national fink.");
		}

		private int InputNumber()
		{
			InputResult result = _chatConnection.InputNumber(":", null, 1);
			return result.InputNumber;
		}

		private int IntRnd(int min, int max)
		{
			return _rnd.Next(min, max + 1);
		}

		private void MsgCanNotDo()
		{
			_chatConnection.SendAscii("\r\ri can not do what you wish.");
		}

		private void MsgNotEnoughBushels(double bushels)
		{
			_chatConnection.SendAscii($"\r\ryou have only {bushels} bushels of grain.");
		}

		private void MsgNotEnoughAcres(double acres)
		{
			_chatConnection.SendAscii($"\r\rthink again. you own only {acres} acres.");
		}

		private void MsgNotEnoughPeople(double people)
		{
			_chatConnection.SendAscii($"\r\ryou have only {people} people to tend the fields! now then,");
		}

	}
}
