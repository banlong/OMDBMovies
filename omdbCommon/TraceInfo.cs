using System;

namespace omdbCommon
{
    public static class TraceInfo {

        public static string ShortDate = GetShortDate();
        public static string ShortTime = GetShortTime();

        private static string GetShortTime() {
            
            return DateTime.Now.ToLongTimeString() + " ";
        }

        private static string GetShortDate() {
            return DateTime.Now.ToShortDateString() + " ";
        }
    }
}
