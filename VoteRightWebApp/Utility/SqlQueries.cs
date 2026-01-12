namespace VoteRightWebApp.Utility;

public static class SqlQueries
{
    public static class Metadata
    {
        public const string DistinctDistricts = @"SELECT DISTINCT district
                                                  FROM public.metadata
                                                  WHERE district IS NOT NULL AND district <> ''
                                                  ORDER BY district";
    }

    public static class Users
    {
        public const string FindByPhone = @"SELECT id, name, phone_number, district
                                           FROM Users WHERE phone_number = @phone LIMIT 1";

        public const string Insert = @"INSERT INTO Users (name, phone_number, whatsapp_number, district, political_party_organization, organizational_position, registered_at)
                                       VALUES (@name, @phone, @wa, @district, @org, @pos, @reg)
                                       RETURNING id";

        public const string SelectByDistrict = @"SELECT DISTINCT id, name, phone_number, whatsapp_number, district, political_party_organization, organizational_position, registered_at
                                                FROM Users WHERE district = @district";

        public const string SelectByDistrictAndAssemblyJoinDownloads = @"SELECT DISTINCT u.id, u.name, u.phone_number, u.whatsapp_number, u.district, u.political_party_organization, u.organizational_position, u.registeredAt
                                                                         FROM Users u INNER JOIN Downloads d ON u.id = d.userId
                                                                         WHERE u.district = @district AND d.assembly = @assembly";
    }

    public static class Downloads
    {
        public const string Insert = @"INSERT INTO Downloads (userId, assembly, booths, deviceType, downloadedAt)
                                      VALUES (@userId, @assembly, @booths, @deviceType, @downloadedAt)
                                      RETURNING id";
    }

    public static class Assemblies
    {                
        public const string DistinctByDistrictWithBoothCount = @"SELECT
                                                                                                                                        v.constituency_details->>'assembly_constituency_name'   AS name,
                                                                                                                                        v.constituency_details->>'assembly_constituency_number' AS number,
                                                                                                                                        COUNT(DISTINCT v.constituency_details->>'part_number')  AS booth_count
                                                                                                                                 FROM public.voters v
                                                                                                                                 JOIN public.metadata m
                                                                                                                                     ON m.part_no::text = v.constituency_details->>'part_number'
                                                                                                                                 WHERE NULLIF(v.constituency_details->>'assembly_constituency_name', '') IS NOT NULL
                                                                                                                                     AND NULLIF(v.constituency_details->>'assembly_constituency_number', '') IS NOT NULL
                                                                                                                                     AND NULLIF(m.district, '') IS NOT NULL
                                                                                                                                     AND m.district = @district
                                                                                                                                 GROUP BY
                                                                                                                                         v.constituency_details->>'assembly_constituency_name',
                                                                                                                                         v.constituency_details->>'assembly_constituency_number'
                                                                                                                                 ORDER BY CAST(v.constituency_details->>'assembly_constituency_number' AS INTEGER)";
    }

    public static class Voters
    {
        public const string SelectByAssemblyBase = @"SELECT document_id, serial_no, epic_no, name, relation_type, father_name, mother_name,
                                                            husband_name, other_name, house_no, age, gender, street_names_and_numbers,
                                                            part_no, assembly, epic_valid, deleted
                                                     FROM public.voters
                                                     WHERE assembly LIKE @assembly";

        public const string RangeClause = " AND CAST(part_no AS INTEGER) BETWEEN @start AND @end";
        public const string OrderByPartNoSerialNo = " ORDER BY part_no, serial_no";
    }
}
