using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Util;
using Android.Widget;
using LiteDB;
using System.Threading.Tasks;
using Java.Interop;
using Android.OS;

namespace PetGo {
	enum PetAction {
		None,
		Idle,
		Sleep,
		Bath,
		GoOut,
		Feed,
		Drink,
		Med,
		Sick,
		UpdateStatus
	}

	[BroadcastReceiver(Label = "PetGo")]
	[IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
	// The "Resource" file has to be all in lower caps
	[MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider")]
	public class AppWidget : AppWidgetProvider {

		private static bool lockstatus = false;
		private static bool isUpdateStatusSetUp = false;

		/// <summary>
		/// This method is called when the 'updatePeriodMillis' from the AppwidgetProvider passes,
		/// or the user manually refreshes/resizes.
		/// </summary>
		public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds) {
			var me = new ComponentName(context, Java.Lang.Class.FromType(typeof(AppWidget)).Name);
			appWidgetManager.UpdateAppWidget(me, BuildRemoteViews(context, appWidgetIds));

		}

		private RemoteViews BuildRemoteViews(Context context, int[] appWidgetIds) {
			// Retrieve the widget layout. This is a RemoteViews, so we can't use 'FindViewById'
			var widgetView = new RemoteViews(context.PackageName, Resource.Layout.Widget);

			//SetTextViewText(widgetView);
			RegisterClicks(context, appWidgetIds, widgetView);

			return widgetView;
		}

		private void SetTextViewText(RemoteViews widgetView) {
			//widgetView.SetTextViewText(Resource.Id.widgetMedium, "PetGo");
			//widgetView.SetTextViewText(Resource.Id.widgetSmall, string.Format("Last update: {0:H:mm:ss}", DateTime.Now));
		}

		private void RegisterClicks(Context context, int[] appWidgetIds, RemoteViews widgetView) {
			var intent = new Intent(context, typeof(AppWidget));
			intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
			intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);

			// Register click event for the Announcement-icon
			widgetView.SetOnClickPendingIntent(Resource.Id.room, GetPendingSelfIntent(context, PetAction.Idle));
			widgetView.SetOnClickPendingIntent(Resource.Id.buttonSleep, GetPendingSelfIntent(context, PetAction.Sleep));
			widgetView.SetOnClickPendingIntent(Resource.Id.buttonBath, GetPendingSelfIntent(context, PetAction.Bath));
			widgetView.SetOnClickPendingIntent(Resource.Id.buttonGoOut, GetPendingSelfIntent(context, PetAction.GoOut));
			widgetView.SetOnClickPendingIntent(Resource.Id.buttonFeed, GetPendingSelfIntent(context, PetAction.Feed));
			widgetView.SetOnClickPendingIntent(Resource.Id.buttonDrink, GetPendingSelfIntent(context, PetAction.Drink));
			widgetView.SetOnClickPendingIntent(Resource.Id.buttonMed, GetPendingSelfIntent(context, PetAction.Med));
			
			Log.Debug("pet", "registerclick");
		}

		private PendingIntent GetPendingSelfIntent(Context context, string action) {
			var intent = new Intent(context, typeof(AppWidget));
			intent.SetAction(action);
			return PendingIntent.GetBroadcast(context, 0, intent, 0);
		}

		private PendingIntent GetPendingSelfIntent(Context context, PetAction petAction) {
			return GetPendingSelfIntent(context, ((int)petAction).ToString());
		}

		/// <summary>
		/// This method is called when clicks are registered.
		/// </summary>
		public override void OnReceive(Context context, Intent intent) {
			base.OnReceive(context, intent);
			Log.Debug("pet", intent.Action);

			// Check if the click is from the "Announcement" button
			if (int.TryParse(intent.Action, out int id)) {
				PetAction action = (PetAction)id;

				int res = PetLogic(action, context);

				if (res == -1) {
					return;
				}

				AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
				var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(AppWidget)).Name);

				RemoteViews remoteViews = new RemoteViews(context.PackageName, Resource.Layout.Widget);

				remoteViews.SetImageViewResource(Resource.Id.room, res);

				appWidgetManager.UpdateAppWidget(component, remoteViews);
			}
		}

		private int PetLogic(PetAction action, Context context) {
			int result = -1;
            //get pet status
			PetAction status = GetPetStatus();
            //pet action which will be scheduled
			PetAction pendingAction = PetAction.Idle;
            //second till pendingAction
			int pendingActionSchedule = 0;
            //get pet info
			Pet pet = GetPetInfo();
            
			Log.Debug("petaction", action.ToString());
			Log.Debug("petstatus", status.ToString());

			switch (action) {
				case PetAction.Idle:
					result = Resource.Drawable.idle_happy;
					if (pet.Happyness < 50) {
						result = Resource.Drawable.idle_unhappy;
					}
					if (pet.Sickness<20) {
						result = Resource.Drawable.sick;
					}
					break;
				case PetAction.Sleep:
					result = Resource.Drawable.sleep;
					pendingActionSchedule = 100;
					break;
				case PetAction.Bath:
					if (pet.Filthyness<100) {
						result = Resource.Drawable.bath_happy;
						pendingActionSchedule = 3;
						pet.Filthyness = 100;
					}
					else {
						result = Resource.Drawable.bath_unhappy;
						pendingActionSchedule = 3;
					}
					break;
				case PetAction.GoOut:
					if (pet.Happyness<100) {
						result = Resource.Drawable.park_happy;
						pendingActionSchedule = 10;
						pet.Happyness = 100;
					}
					else {
						result = Resource.Drawable.park_unhappy;
						pendingActionSchedule = 3;
					}
					break;
				case PetAction.Feed:
					if (pet.Hunger<100) {
						result = Resource.Drawable.eat_happy;
						pendingActionSchedule = 4;
						pet.Hunger = 100;
					}
					else {
						result = Resource.Drawable.eat_unhappy;
						pendingActionSchedule = 2;
					}
					break;
				case PetAction.Drink:
					if (pet.Thirst<100) {
						result = Resource.Drawable.drink_happy;
						pendingActionSchedule = 3;
						pet.Thirst = 100;
					}
					else {
						result = Resource.Drawable.drink_unhappy;
						pendingActionSchedule = 2;
					}
					break;
				case PetAction.Med:
					if (pet.Sickness<100) {
						result = Resource.Drawable.med_happy;
						pendingActionSchedule = 5;
						pet.Sickness = 100;
					}
					else {
						result = Resource.Drawable.med_unhappy;
						pendingActionSchedule = 2;
					}
					break;
				case PetAction.Sick:
					result = Resource.Drawable.sick;
					break;
				case PetAction.UpdateStatus:
					result = -1;
					pendingActionSchedule = 10;
					pet.Hunger -= 10;
					pet.Thirst -= 10;
					pet.Happyness -= 10;
					pet.Sleepyness -= 10;
					pet.Filthyness -= 10;
					pet.Sickness -= 50;
					isUpdateStatusSetUp = false;
					break;
				default:
					break;
			}

            //set pet status in database
			SetPetStatus(action);

            //schedule update status
			if (!isUpdateStatusSetUp) {
				SchedulePetAction(10, PetAction.UpdateStatus, context);
				isUpdateStatusSetUp = true;
			}

            //schedule pendingAction
			if (pendingActionSchedule !=0) {
				SchedulePetAction(pendingActionSchedule, pendingAction, context);
			}

            //set pet info in database
			SetPetInfo(pet);

			return result;
		}

		private void SchedulePetAction(int second, PetAction petAction, Context context) {
			PendingIntent pendingIntent = GetPendingSelfIntent(context, petAction);
			MainActivity.alarmManager.Set(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + second * 1000, pendingIntent);
		}

		private PetAction GetPetStatus() {
			using (var db = new LiteDatabase(Constant.dbpath)) {
				var pet = db.GetCollection<Pet>("pet").FindOne(Query.Contains("Name", "lala"));
				return (PetAction)pet.Status;
			}
		}

		private bool SetPetStatus(PetAction status) {
			using (var db = new LiteDatabase(Constant.dbpath)) {
				var pet = db.GetCollection<Pet>("pet").FindOne(Query.Contains("Name", "lala"));
				pet.Status = (int)status;
				return db.GetCollection<Pet>("pet").Update(pet);
			}
		}

		private Pet GetPetInfo() {
			using (var db = new LiteDatabase(Constant.dbpath)) {
				var pet = db.GetCollection<Pet>("pet").FindOne(Query.Contains("Name", "lala"));
				return pet;
			}
		}

		private bool SetPetInfo(Pet pet) {
			using (var db = new LiteDatabase(Constant.dbpath)) {
				return db.GetCollection<Pet>("pet").Update(pet);
			}
		}
	}
}
