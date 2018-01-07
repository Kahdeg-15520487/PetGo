using Android.App;
using Android.Widget;
using Android.OS;
using LiteDB;
using System.IO;
using Android.Content;

namespace PetGo {
	public class Pet {
		public int Id { get; set; }
		public string Name { get; set; }
		public int Status { get; set; }
		public int Hunger { get; set; }
		public int Thirst { get; set; }
		public int Happyness { get; set; }
		public int Sickness { get; set; }
		public int Sleepyness { get; set; }
		public int Filthyness { get; set; }
	}

	public class Constant {
		public static string dbpath = "";
	}
	[Activity(Label = "PetGo Widget", MainLauncher = true, Icon = "@drawable/android")]
	public class MainActivity : Activity {

		public static AlarmManager alarmManager;

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			alarmManager = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			Toast.MakeText(this, "Long-press the homescreen to add the widget", ToastLength.Long).Show();
			var documentspath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
			var dbpath = Path.Combine(documentspath, "pet.db");
			Constant.dbpath = dbpath;
			using (var db = new LiteDatabase(dbpath)) {
				db.DropCollection("pet");
				var data = db.GetCollection<Pet>("pet");
				var pet = new Pet() {
					Name = "lala",
					Status = (int)PetAction.Idle,
					Hunger = 100,
					Thirst = 100,
					Happyness = 100,
					Sickness = 100,
					Sleepyness = 100,
					Filthyness = 100
				};
				data.Insert(pet);
			}
			Finish();
		}
	}
}

