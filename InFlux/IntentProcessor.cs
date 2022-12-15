using System;
using System.Collections.Generic;

namespace InFlux
{
    /// <summary>
    /// register as SCOPED.  every intent is user specific (connection / circuit).
    /// <para>
    /// This is intented (no pub intended. (d'oh!)) to be THE ONE intent processor that you can use throughout
    /// your application.  Given subscriptions have filtering fields, you will only get involved in the intents you wish
    /// to hear about.
    /// Subscriptions are weak references, so shouldn't prevent components being garbage collected.
    /// </para>
    /// </summary>
    public class IntentProcessor
    {
        #region constructors

        /// <summary>
        /// instantiation requires ability to fire an ALE event that should trigger a response from the user
        /// which either grants or denies permission to perform an Action.
        /// </summary>
        /// <param name="ale_AskUserForPermission">
        /// If you provide an Application Level Event (ALE) that relates to asking the user for permission,
        /// of which the payload for the event, is an Action that takes a boolean parameter (the user response)
        /// then this constructor will hook up to that for you, as needed.
        /// </param>
        public IntentProcessor(SimpleQueuedEvent<Action<bool>> ale_AskUserForPermission)
        {
            fireAskForPermissionEvent = callback => ale_AskUserForPermission.FireEvent(callback);
        }

        /// <summary>
        /// instantiation requires ability to fire an ALE event that should trigger a response from the user
        /// which either grants or denies permission to perform an Action.
        /// </summary>
        /// <param name="fireAskForPermissionEventWithCallbackForResponse">
        /// PERMISSION NEEDED:
        ///    Subscrible ONCE to permission, which should have a callback to here for success / failure for
        ///    permission from the user for the change.
        /// </param>
        public IntentProcessor(Action<Action<bool>> fireAskForPermissionEventWithCallbackForResponse)
        {
            fireAskForPermissionEvent = fireAskForPermissionEventWithCallbackForResponse;
        }

        #endregion

        private readonly Action<Action<bool>> fireAskForPermissionEvent;

        private class Registration
        {
            public Registration(string callerType, string callerName, Action<Intent<object>> nonGenericModeratorCode)
            {
                CallerType = callerType;
                CallerName = callerName;
                NonGenericModeratorCode = nonGenericModeratorCode;
            }

            public string CallerType { get; private set; }
            public string CallerName { get; private set; }
            public Action<Intent<object>> NonGenericModeratorCode { get; private set; }
        }

        private readonly List<WeakReference<Registration>> subscribersForAllIntents = new List<WeakReference<Registration>>();
        private readonly List<WeakReference<Registration>> oneOffSubscribersForAllIntents = new List<WeakReference<Registration>>();

        /// <summary>
        /// If you've some business logic reason for possibly wanting to prevent a change from happening,
        /// this is where you can join in the process where you will be informed of the intended change, and
        /// your logic can determine if the user should be asked for permission for such a change - for example,
        /// if you know that you have unsaved changes that the user will not be able to recover, if this change
        /// is allowed to go ahead.
        /// <para>
        /// The <paramref name="moderatorCode"/> you provide should expect an <see cref="Intent{T}"/> to be given.
        /// This provides you with the old value (current value) and the new value (intended change) from which
        /// you can help determine if the user should be asked before this change goes ahead.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callerType">Consider this a kind of context in which the Intent will exist - for example, relating to a web page.</param>
        /// <param name="callerName">As a secondary refinement, there could be many Intentions on a page, this this should uniquely identify an Intent within a web page.</param>
        /// <param name="moderatorCode">Your code that can determine if the user needs to be asked for permission by calling the <see cref="Intent{T}.AskUserForPermission"/> method</param>
        public void SubscribeToIntentProcess<T>(string callerType, string callerName, Action<Intent<T>> moderatorCode)
        {
            subscribersForAllIntents.Add(
                prepareForSubscription(callerType, callerName, moderatorCode));
        }

        /// <summary>
        /// If you've some business logic reason for possibly wanting to prevent a change from happening the very next time its attempted,
        /// this is where you can join in the process where you will be informed of the intended change, and
        /// your logic can determine if the user should be asked for permission for such a change - for example,
        /// if you know that you have unsaved changes that the user will not be able to recover, if this change
        /// is allowed to go ahead.
        /// <para>
        /// The <paramref name="moderatorCode"/> you provide should expect an <see cref="Intent{T}"/> to be given.
        /// This provides you with the old value (current value) and the new value (intended change) from which
        /// you can help determine if the user should be asked before this change goes ahead.
        /// </para>
        /// </summary>
        /// <param name="callerType">Consider this a kind of context in which the Intent will exist - for example, relating to a web page.</param>
        /// <param name="callerName">As a secondary refinement, there could be many Intentions on a page, this this should uniquely identify an Intent within a web page.</param>
        /// <param name="moderatorCode">Your code that can determine if the user needs to be asked for permission by calling the <see cref="Intent{T}.AskUserForPermission"/> method</param>

        public void SubscribeToIntentProcessOnceOnly<T>(string callerType, string callerName, Action<Intent<T>> moderatorCode)
        {
            oneOffSubscribersForAllIntents.Add(
                prepareForSubscription(callerType, callerName, moderatorCode));
        }

        /// <summary>
        /// If you Intend to change a value, and need to know if there is some business-logic reason why the change should be challenged,
        /// and the user be asked for permission to make such a change, this is where you start!  You provide filter values for this Intent,
        /// so subscribers can be connected with your Intent to change a value.  You also provide the current (old) value as well as the
        /// new value (intended), and code that will change this value if you're deemed allowed to do so.  You can optionally provide code
        /// that is called if you are denied by the user to make the change.
        /// </summary>
        /// <param name="callerType">Consider this a kind of context in which the Intent will exist - for example, relating to a web page.</param>
        /// <param name="callerName">As a secondary refinement, there could be many Intentions on a page, this this should uniquely identify an Intent within a web page.</param>
        /// <param name="oldValue">the current value you intend to overwrite, this consider it to be the old value.</param>
        /// <param name="newValue">the new value that should replace the old value, if you're allowed to make that change.</param>
        /// <param name="codeIfAllowed">Code that is called only if you're allowed - which changes the value referred to as the new value.</param>
        /// <param name="callbackIfNotAllowed">Code this is called if you were not allowed to make the intended change.</param>
        public void FireIntent<T>(string callerType, string callerName, T oldValue, T newValue, Action codeIfAllowed, Action callbackIfNotAllowed = null)
        {
            // 1. Identify subscribers for this intent.
            var listeners = identifyInterestedModerators(callerType, callerName);

            // 2. Call each subscriber in turn, and watch for permission requirement at same time.
            bool permissionNeeded = CallModeratorsToDetermineIfWeNeedToAskForPermission(listeners, oldValue, newValue);

            // 3. If no permission needed, CLEAR One-offs, then call the Action, and RETURN.
            oneOffSubscribersForAllIntents.Clear();

            if (permissionNeeded == false)
            {
                codeIfAllowed();
                return;
            }

            // 4. PERMISSION NEEDED:
            //    Subscrible once to permission, which should have a callback to here for success / failure for
            //    permission from the user for the change.

            fireAskForPermissionEvent(userGivesPermission =>
            {
                // 5. If permission granted, call the action, and RETURN.
                if (userGivesPermission)
                {
                    codeIfAllowed();
                }
                else
                {
                    // 6. If permission denied, call the denial code and RETURN.
                    callbackIfNotAllowed?.Invoke();
                }
            });
        }

        private WeakReference<Registration> prepareForSubscription<T>(string callerType, string callerName, Action<Intent<T>> moderatorCode)
        {
            /* I know this seems monsterously complicated, but we have to register against a non-generic collection,
             * and that means the object (intent) needs conversion to a generic version in order to call the subscriber
             * code.  This then must feedback through the non generic "intent" if the user should be asked for permission.
             */
            var reg = new Registration(callerType, callerName, nonGenericModeratorCode:
                    intent =>
                    {
                        var moderatorPayload = new Intent<T>((T)intent.OldValue, (T)intent.NewValue);
                        moderatorCode(moderatorPayload);
                        if (moderatorPayload.PermissionNeededForChange)
                            intent.AskUserForPermission();
                    }
                );

            return new WeakReference<Registration>(reg);
        }

        private List<Registration> identifyInterestedModerators(string callerType, string callerName)
        {
            var listeners = new List<Registration>();
            subscribersForAllIntents.ForEach(wr =>
            {
                if (wr.TryGetTarget(out var reg))
                    if (reg != null && reg.CallerType == callerType)
                        if (callerName is null || (reg != null && reg.CallerName == callerName))
                            listeners.Add(reg);
            });
            oneOffSubscribersForAllIntents.ForEach(wr =>
            {
                if (wr.TryGetTarget(out var reg))
                    if (reg != null && reg.CallerType == callerType)
                        if (callerName is null || (reg != null && reg.CallerName == callerName))
                            listeners.Add(reg);
            });

            return listeners;
        }

        private bool CallModeratorsToDetermineIfWeNeedToAskForPermission(List<Registration> listeners, object oldValue, object newValue)
        {
            var intent = new Intent<object>(oldValue, newValue);

            foreach (var listener in listeners)
            {
                listener.NonGenericModeratorCode(intent);
                if (intent.PermissionNeededForChange)
                    break;
            }

            return intent.PermissionNeededForChange;
        }
    }
}
