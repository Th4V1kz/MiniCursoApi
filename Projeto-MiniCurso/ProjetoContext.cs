using Projeto_MiniCurso.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace Projeto_MiniCurso

{
    public class ProjetoContext : DbContext
    {
        public ProjetoContext(DbContextOptions<ProjetoContext> options)
            : base(options) { }

        public DbSet<Aluno> Alunos { get; set; }
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<AlunoCurso> AlunosCursos { get; set; }
    }
}
