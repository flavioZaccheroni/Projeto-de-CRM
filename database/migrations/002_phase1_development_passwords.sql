update users
set password_hash = 'dev:123456'
where password_hash = 'trocar-por-hash-real';
