using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_MiniCurso.Models;

namespace Projeto_MiniCurso.Controllers

{
   [ApiController]
    [Route("api/[controller]")]
    public class AlunoController : ControllerBase
    {
        public readonly ProjetoContext _context;

        public AlunoController(ProjetoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Aluno>>> GetAll()
        {
            return await _context.Alunos.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Aluno>> GetById(int id)
        {
            var aluno = await _context.Alunos.FirstOrDefaultAsync(a => a.Id == id);

            if (aluno == null) return NotFound();

            return Ok(aluno);
        }
    

        [HttpPost]
        public async Task<ActionResult<Aluno>> Create(Aluno aluno)
        {
            _context.Alunos.Add(aluno);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = aluno.Id }, aluno);
        }
    }
}
