# Innovation4Albania.Lab

Versioni `Lab` i platformes `Innovation4Albania`, i pergatitur per:

- zhvillim lokal
- publikim ne GitHub
- deploy ne Render

## Cfare perfshin

- backend `ASP.NET Core`
- frontend statik ne `wwwroot`
- dashboard sipas roleve
- module operative per projekte, dokumente, workflow, detyra dhe OKR
- funksione AI per risk, summary, smart alerts dhe chat

## Rolet demo

- `prime-minister` - Kryeministri
- `minister` - Ministrja
- `director` - Drejtori i Pergjithshem
- `nuklis-director` - Drejtori i NUKLIS-it
- `expert-mdig` - Eksperti

## Nisja lokale

1. Hape solution-in:
   `Innovation4Albania.Lab.slnx`
2. Zgjidh startup project:
   `Innovation4Albania.API`
3. Run / F5

Ose me terminal:

```powershell
dotnet run --project Innovation4Albania.API\Innovation4Albania.API.csproj
```

## GitHub

Per ta kthyer ne repo me vete dhe per ta lidhur me GitHub:

```powershell
cd "C:\Users\enkel\OneDrive\Documents\New project\Innovation4Albania.Lab"
git init
git checkout -b main
git add .
git commit -m "Initial commit"
git remote add origin https://github.com/USERNAME/REPO_NAME.git
git push -u origin main
```

Nese repo ekziston tashme ne GitHub, krijoje bosh pa `README`, pa `.gitignore` dhe pa license, pastaj lidhja me `remote` do te funksionoje pa konflikt.

## Render

Ky projekt eshte pergatitur per Render me:

- `Dockerfile`
- `render.yaml`

### Menyra me e thjeshte

1. Beje push projektin ne GitHub.
2. Hape [Render](https://render.com/).
3. Zgjidh `New +` -> `Blueprint`.
4. Lidhe me repository-n e GitHub.
5. Render do te lexoje `render.yaml` dhe do te krijoje sherbimin automatikisht.

### Nese do manualisht

1. `New +` -> `Web Service`
2. Zgjidh repo-n nga GitHub
3. Runtime: `Docker`
4. Root directory: bosh
5. Dockerfile path:
   `./Dockerfile`

## Vercel

Ky projekt nuk eshte i pershtatshem per deploy te plote ne Vercel si eshte aktualisht, sepse:

- frontend dhe backend jane bashke ne nje aplikacion `ASP.NET Core`
- API dhe `wwwroot` sherbehen nga i njejti project

Per kete arsye rekomandohet:

- `GitHub + Render` per gjithe aplikacionin

## File te rendesishem per deploy

- `Dockerfile`
- `render.yaml`
- `Innovation4Albania.API/Program.cs`
- `Innovation4Albania.API/Innovation4Albania.API.csproj`

## Shenim

Per deploy ne Render, nese perdoren funksionet AI, celesat perkates duhet te shtohen si environment variables ne panelin e Render.
