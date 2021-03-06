using AccountsAPI.Dtos;
using AccountsAPI.Models;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountsAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class VoucherController : ControllerBase
    {
        private AccountsDBContext _context;
        private IMapper _mapper;

        public VoucherController(AccountsDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        [HttpPost]
        public IActionResult AddVoucher(CreateVoucherDto voucherdto )
        {
            decimal drsum =(decimal) voucherdto.transactions.Where(w => w.Direction.ToLower() == "dr").Sum(s => s.Amount);
            decimal crsum =(decimal) voucherdto.transactions.Where(w => w.Direction.ToLower() == "cr").Sum(s => s.Amount);
           
            if (drsum != crsum)
                return BadRequest("Credit and Debit Amounts dont match...");


            Voucher voucher = _mapper.Map<Voucher>(voucherdto.voucher);
            List<AccountTransaction> transactions = _mapper.Map<List<AccountTransaction>>(voucherdto.transactions);
            voucher.Voucherdate = DateTime.Now;
            _context.Vouchers.Add(voucher);
            _context.SaveChanges();

            GeneralJournal generalJournal = new GeneralJournal();
            generalJournal.Voucherid = voucher.Id;
            _context.GeneralJournals.Add(generalJournal);
            _context.SaveChanges();

            transactions.ForEach(fe => fe.Gjentry = generalJournal.Id);
            _context.AccountTransactions.AddRange(transactions);
            _context.SaveChanges();

            ReadVoucherDto readVoucher = _mapper.Map<ReadVoucherDto>(voucher);
            readVoucher.Amount =(decimal) _context.AccountTransactions.Where(w => w.Gjentry == generalJournal.Id && w.Direction.ToLower().Equals("dr")).Sum(s => s.Amount);


            return CreatedAtRoute(nameof(GetVoucherById), new { id = readVoucher.Id }, readVoucher);
        }
        [HttpPost]
        public IActionResult DeleteVoucher(int id)
        {
            Voucher voucher = _context.Vouchers.Where(w => w.Id == id).FirstOrDefault();
            if (voucher == null)
                return BadRequest();

            voucher.Status = false;
            _context.Vouchers.Update(voucher);
            _context.SaveChanges();

            return Ok();
        }
        [HttpGet(Name = "GetVoucherById")]
        public IActionResult GetVoucherById(int id)
        {
            Voucher voucher = _context.Vouchers.Where(w => w.Id == id && w.Status == true).FirstOrDefault();
            if (voucher == null)
                return NotFound();

            ReadVoucherDto retdto = _mapper.Map<ReadVoucherDto>(voucher);
            GeneralJournal generalentry = _context.GeneralJournals.Include(i => i.AccountTransactions).Where(w => w.Voucherid == id).First();
            retdto.Amount=(decimal) generalentry.AccountTransactions.Where(w => w.Direction.ToLower() == "dr").Sum(s => s.Amount);

            return Ok(retdto);
        }
        [HttpGet]
        public IActionResult GetAllVocuhers(int vouchertype =0)
        {
            List<Voucher> vouchers;

            if (vouchertype != 0)
                vouchers = _context.Vouchers.Where(w => w.Vouchertype == vouchertype && w.Status == true).ToList();
            else
                vouchers = _context.Vouchers.Where(w => w.Status == true).ToList();

            List< GeneralJournal> gjs = _context.GeneralJournals.ToList();

            var enter2 = gjs.Where(w => vouchers.Select(s => s.Id).ToList().Contains((int)w.Voucherid)).Select(se => new { gen = se.Id, vou = se.Voucherid }).ToList();

            var enter = gjs.Where(w => vouchers.Select(s => s.Id).ToList().Contains((int)w.Voucherid)).Select(se=> se.Id ).ToList();

            var li= _context.AccountTransactions.Where(w => enter.Contains((int)w.Gjentry) && w.Direction.ToLower() == "dr").GroupBy(g => g.Gjentry).Select(s => new { s.Key, sum= s.Sum(su=>su.Amount)}).ToList();
  
            List<ReadVoucherDto> readVoucherDtos = new List<ReadVoucherDto>();
            _mapper.Map(vouchers, readVoucherDtos);

            readVoucherDtos.ForEach(fe =>
            {
                int generalid =(int) enter2.Where(w => w.vou == fe.Id).Select(s=>s.gen).First();
                fe.Amount = (decimal)li.Where(w => w.Key == generalid).Select(s => s.sum).First();
            });


            return Ok(readVoucherDtos);
        }
    }
}
