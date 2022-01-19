using CountDown.Models;
using CountDown.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Collections.Specialized;

namespace CountDown.Controllers
{
    public class HomeController : Controller
    {
        GiftsEntities _db = new GiftsEntities();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetQualify(string _Location)
        {
            Uri uri = new Uri(_Location);
            string queryString = uri.Query;
            if (!string.IsNullOrEmpty(queryString) && queryString.Contains("?"))
            {
                queryString = queryString.Replace("?", string.Empty);
                queryString = queryString.Replace("%20", string.Empty);
            }

            var nvc = HttpUtility.ParseQueryString(queryString);
            string _MemberId = GetParam(nvc, "feid");

            Results results = new Results();
            DateTime _today = DateTime.Now;
            var _json = results;

            //兌換名單
            EntityPresent entityPresent = _db.EntityPresent.Where(o => o.MemberId == _MemberId && o.Status == "N" && o.UsedStart <= _today && _today <= o.UsedEnd).FirstOrDefault();
            if (entityPresent == null)
            {
                results.Status = false;
                results.Message = "此會員無兌換資格！";
                _json = results;

            }
            else
            {
                results = new Results
                {
                    Status = true
                };
                _json = results;
            }
            return Json(_json);
        }

        public ActionResult GetCheck(string _Location, string PosId)
        {
            Uri uri = new Uri(_Location);
            string queryString = uri.Query;
            if (!string.IsNullOrEmpty(queryString) && queryString.Contains("?"))
            {
                queryString = queryString.Replace("?", string.Empty);
                queryString = queryString.Replace("%20", string.Empty);
            }

            var nvc = HttpUtility.ParseQueryString(queryString);
            string _MemberId = GetParam(nvc, "feid");

            DateTime _today = DateTime.Now;
            Results results = new Results();

            results = new Results
            {
                Status = false,
                Message = "錯誤！",
                Mid = "",
                Gid = "",
                MallId = ""
            };

            var _json = results;

            //櫃號(檢核碼)
            //switch (PosId)
            //{
            //    case "540144": PosId = "540158"; break;
            //    case "500148": PosId = "500150"; break;
            //    case "320122": PosId = "320134"; break;
            //    case "530157": PosId = "530166"; break;
            //    case "400134": PosId = "400126"; break;
            //    case "420107": PosId = "420196"; break;
            //    case "420121": PosId = "420196"; break;
            //    case "510135": PosId = "510149"; break;
            //    case "520116": PosId = "520115"; break;
            //}

            //測試
            //int GId = 1462;
            //int GId2 = ;
            //正式
            int GId = 4262;
            //int GId2 = ;

            PosIdList posIdList = _db.PosIdList.Where(o => o.PosId == PosId && o.GId == GId ).FirstOrDefault();
            if (posIdList == null)
            {
                results.Message = "檢核碼錯誤！";
                _json = results;
                return Json(_json);
            }
            else
            {
                //兌換名單
                EntityPresent entityPresent = _db.EntityPresent.Where(o => o.GId == posIdList.GId && o.MemberId == _MemberId && o.Status == "N" && o.UsedStart <= _today && _today <= o.UsedEnd).FirstOrDefault();
                if (entityPresent == null)
                {
                    results.Message = "此會員無兌換資格或已領取過！";
                    _json = results;
                    return Json(_json);
                }
                else
                {
                    results = new Results
                    {
                        Status = true,
                        Message = entityPresent.CouponNo,
                        Mid = _MemberId,
                        Gid = entityPresent.GId.ToString(),
                        MallId = posIdList.MallId,
                        DepartmentNo = posIdList.DepartmentNo
                    };
                    _json = results;
                }
            }

            return Json(_json);
        }


        public string GetParam(NameValueCollection nvc, string key)
        {
            var result = nvc.Get(key);
            return result;
        }

        public ActionResult ExchangeGifts(Results results)
        {
            DateTime _today = DateTime.Now;
            var _json = new { Status = false, Message = "兌換失敗！", DeptNo = "" };

            int _gid = Convert.ToInt32(results.Gid);
            int? _erid = _db.Exchange_Gifts.Where(o => o.GId == _gid).OrderByDescending(o => o.Id).FirstOrDefault().ERId;

            EntityPresent entityPresent = _db.EntityPresent.Where(o => o.GId == _gid && o.MemberId == results.Mid && o.Status == "N" && o.UsedStart <= _today && _today <= o.UsedEnd).FirstOrDefault();
            ExchangeMall exchangeMall = _db.ExchangeMall.Where(o => o.ERId == _erid && o.MallId == results.MallId).FirstOrDefault();
            if (entityPresent == null)
            {
                return Json(_json);
            }
            else if (exchangeMall.Quantity == 0)
            {
                //贈品數量為0
                return Json(_json);
            }
            else
            {
                entityPresent.MallId = results.MallId;
                entityPresent.Status = "U";
                entityPresent.UsedDate = _today;
                entityPresent.UsedDepartment = results.DepartmentNo;
                exchangeMall.Quantity = exchangeMall.Quantity - 1;
                _db.Entry(entityPresent).State = EntityState.Modified;
                _db.Entry(exchangeMall).State = EntityState.Modified;

                _db.EntityPresent_Log.Add(new EntityPresent_Log
                {
                    MemberId = results.Mid,
                    MallId = results.MallId,
                    GId = Convert.ToInt32(results.Gid),
                    CouponNo = entityPresent.CouponNo,
                    Status = "U",
                    CreateOn = _today,
                    UsedMallId = results.MallId,
                    UsedDepartment = results.DepartmentNo,
                    Remark = _today.Month + "月份會員生日禮"
                });

                _db.SaveChanges();
                _json = new { Status = true, Message = "兌換成功！", DeptNo = results.DepartmentNo };
            }
            return Json(_json);
        }

    }
}