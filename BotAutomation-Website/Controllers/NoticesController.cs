using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BotAutomation_Website.Data;
using BotAutomation_Website.Models;

namespace BotAutomation_Website.Controllers
{
    public class NoticesController : Controller
    {
        private readonly BotAutomation_WebsiteContext _context;

        public NoticesController(BotAutomation_WebsiteContext context)
        {
            _context = context;
        }

        // GET: Notices
        public async Task<IActionResult> Index()
        {
              return _context.Notice != null ? 
                          View(await _context.Notice.ToListAsync()) :
                          Problem("Entity set 'BotAutomation_WebsiteContext.Notice'  is null.");
        }

        // GET: Notices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Notice == null)
            {
                return NotFound();
            }

            var notice = await _context.Notice
                .FirstOrDefaultAsync(m => m.Id == id);
            if (notice == null)
            {
                return NotFound();
            }

            return View(notice);
        }

        // GET: Notices/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Notices/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Subject,Message,ItemPath,ScheduledTime")] Notice notice)
        {
            if (ModelState.IsValid)
            {
                _context.Add(notice);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(notice);
        }

        // GET: Notices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Notice == null)
            {
                return NotFound();
            }

            var notice = await _context.Notice.FindAsync(id);
            if (notice == null)
            {
                return NotFound();
            }
            return View(notice);
        }

        // POST: Notices/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Subject,Message,ItemPath,ScheduledTime")] Notice notice)
        {
            if (id != notice.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(notice);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NoticeExists(notice.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(notice);
        }

        // GET: Notices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Notice == null)
            {
                return NotFound();
            }

            var notice = await _context.Notice
                .FirstOrDefaultAsync(m => m.Id == id);
            if (notice == null)
            {
                return NotFound();
            }

            return View(notice);
        }

        // POST: Notices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Notice == null)
            {
                return Problem("Entity set 'BotAutomation_WebsiteContext.Notice'  is null.");
            }
            var notice = await _context.Notice.FindAsync(id);
            if (notice != null)
            {
                _context.Notice.Remove(notice);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NoticeExists(int id)
        {
          return (_context.Notice?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
